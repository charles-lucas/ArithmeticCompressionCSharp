using System.Text.Json;

namespace ArithmeticCoder
{
    internal class CompressionEventArgs : EventArgs
    {
        public CompressionEventArgs(bool complete)
        {
            _complete = complete;
        }

        public bool Complete => _complete;

        private readonly bool _complete;
    }

    internal class CompressArgs
    {
        public CompressArgs(Queue<Int32> input, List<byte> output, Int32 packetSize, Int32 emptyBitsInLastByte = 0, bool padToSize = false)
        {
            _input = input;
            _output = output;
            _packetSize = packetSize;
            _emptyBitsInLastByte = emptyBitsInLastByte;
            _padToSize = padToSize;
        }

        public Queue<Int32> Input => _input;
        public List<byte> Output => _output;
        public Int32 PacketSize => _packetSize;
        public Int32 EmptyBitsInLastByte => _emptyBitsInLastByte;
        public bool PadToSize => _padToSize;

        private Queue<Int32> _input;
        private List<byte> _output;
        private Int32 _packetSize;
        private Int32 _emptyBitsInLastByte;
        private bool _padToSize;
    }

    public class ArithmeticCompression
    {
        //Ctor for loading a model from JSON
        public ArithmeticCompression(Stream modelStream, bool compatabilityMode, bool staticModel = false)
        {
            StreamReader inputReader = new StreamReader(modelStream);
            string? json;
            ModelOrderN? model;
            ContextKey? contextKey;
            Context? context;
            bool done = false;
            _compatabilityMode = compatabilityMode;
            _static = staticModel;
            
            _model = new ModelOrderN(0);

            json = inputReader.ReadLine();
            if(json != null)
            {
                if (staticModel)
                {
                    model = JsonSerializer.Deserialize<ModelOrderN_Static>(json);
                }
                else
                {
                    model = JsonSerializer.Deserialize<ModelOrderN>(json);
                }
                if(model != null)
                {
                    _model = model;
                }
            }

            while(!done)
            {
                //Process ContextKey
                json = inputReader.ReadLine();
                if(json != null)
                {
                    contextKey = JsonSerializer.Deserialize<ContextKey>(json);
                }
                else
                {
                    contextKey = null;
                    done = true;
                }

                //Proicess Context
                json = inputReader.ReadLine();
                if(json != null)
                {
                    context = JsonSerializer.Deserialize<Context>(json);
                }
                else
                {
                    context = null;
                    done = true;
                }

                if(contextKey != null && context != null)
                {
                    _model.Contexts.Add(contextKey, context);
                }
            }

            _compatabilityMode = _model.CompatabilityMode;
        }

        public ArithmeticCompression(UInt32 maxOrder, bool compatabilityMode = false)
        {
            _model = new ModelOrderN(maxOrder, compatabilityMode);
            _compatabilityMode = compatabilityMode;
            _static = false;
        }

        /*
        * The main procedure is similar to the main found in ARITH1E.C.  It has
        * to initialize the coder and the model.  It then sits in a loop
        reading
        * input symbols and encoding them.  One difference is that every 256
        * symbols a compression check is performed.  If the compression ratio
        * falls below 10%, a flush character is encoded.  This flushes the
        encod
        * ing model, and will cause the decoder to flush its model when the
        * file is being expanded.  The second difference is that each symbol is
        * repeatedly encoded until a successful encoding occurs.  When trying
        to
        * encode a character in a particular order, the model may have to
        * transmit an ESCAPE character.  If this is the case, the character has
        * to be retransmitted using a lower order.  This process repeats until
        a
        * successful match is found of the symbol in a particular context.
        * Usually this means going down no further than the order -1 model.
        * However, the FLUSH and DONE symbols drop back to the order -2 model.
        *
        */
        public void Compress(string inputFileName, string outputFileName)
        {
            BinaryReader input = new BinaryReader(File.OpenRead(inputFileName));
            BinaryWriter output = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
            Compress(input, output);
            input.Close();
            output.Flush();
            output.Close();
        }

        public void Compress(BinaryReader input, BinaryWriter output)
        {
            Int32 character;
            Symbol symbol = new Symbol();
            bool escaped;
            bool flush = false;
            Int16 textCount = 0;
            _coder = new Coder(true, input, output, _compatabilityMode);

            while (true)
            {
                if (!_static && (++textCount & 0x0ff) == 0)
                {
                    flush = CheckCompression();
                }

                if (!flush)
                {
                    try
                    {
                        character = input.ReadByte();
                        CompressionTracker.Instance.IncrementInput();
                    }
                    catch (EndOfStreamException)
                    {
                        _model.SetLastContext();
                        character = Constants.DONE;
                    }
                }
                else
                {
                    character = Constants.FLUSH;
                }

                do
                {
                    escaped = _model.ConvertIntToSymbol(character, symbol);
                    _coder.Encode(symbol);
                } while (escaped);

                if (character == Constants.FLUSH)
                {
                    _model.Flush();
                    flush = false;
                }
                else if (character == Constants.DONE)
                {
                    break;
                }

                _model.Update(character);
                _model.AddSymbol(character);
            }
            _coder.Flush();
        }

        public async Task<bool> CompressSplit(BinaryReader input, string outputBaseName, Int32 partMax, bool padToSize = false, Int32 emptyBitsInLastByte = 0)
        {
            bool done = false;
            Int32 partNumber = 0;
            bool test;
            bool outOfData;
            List<byte> outputList;
            Queue<Int32> inputQueue = new Queue<Int32>();
            Int32 bytesToRead = partMax * 2;
            string outputFileName;
            BinaryWriter output;

            do
            {
                outputList = new List<byte>();
                do
                {
                    outOfData = false;
                    inputQueue.Clear();
                    for (int i = 0; !outOfData && i < bytesToRead; i++)
                    {
                        try
                        {
                            inputQueue.Enqueue(input.ReadByte());
                        }
                        catch (EndOfStreamException)
                        {
                            if(inputQueue.Count == 0)
                            {
                                done = true;
                            }
                            outOfData = true;
                        }
                    }
                    test = await CompressPacketAsync(inputQueue, outputList, partMax, emptyBitsInLastByte, padToSize);
                }
                while (!test);
                //write out data
                outputFileName = String.Format("{0}-part{1}.bin", outputBaseName, partNumber++);
                output = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
                for (int i = 0; i < outputList.Count; i++)
                {
                    output.Write(outputList[i]);
                }
                output.Flush();
                output.Close();
            } while (!done);
            //outputList = compressor.CompressPacket(inputQueue, partMax);
            input.Close();

            return true;
        }

        private void Compress(List<Int32> input, List<byte> output, Int32 emptyBitsInLastByte = 0)
        {
            Int32 character;
            Symbol symbol = new Symbol();
            bool escaped;
            bool flush = false;
            Int32 index = 0;
            Int16 textCount = 0;

            while (index < input.Count)
            {
                if (!_static && (++textCount & 0x0ff) == 0)
                {
                    flush = CheckCompression();
                }

                if (!flush)
                {
                    character = input[index++];
                    CompressionTracker.Instance.IncrementInput();
                }
                else
                {
                    character = Constants.FLUSH;
                }

                do
                {
                    escaped = _model.ConvertIntToSymbol(character, symbol);
                    _coder?.Encode(symbol, output);
                } while (escaped);

                if (character == Constants.FLUSH)
                {
                    _model.Flush();
                    flush = false;
                }
                else if (character == Constants.DONE)
                {
                    break;
                }

                _model.Update(character);
                _model.AddSymbol(character);
            }
            _coder?.Flush(output, emptyBitsInLastByte);
        }

        /*
        * The main loop for expansion is very similar to the expansion
        * routine used in the simpler compression program, ARITH1E.C.  The
        * routine first has to initialize the the arithmetic coder and the
        * model.  The decompression loop differs in a couple of respect.
        * First of all, it handles the special ESCAPE character, by
        * removing them from the input bit stream but just throwing them
        * away otherwise.  Secondly, it handles the special FLUSH character.
        * Once the main decoding loop is done, the cleanup code is called,
        * and the program exits.
        *
        */
        public void Expand(BinaryReader input, BinaryWriter output)
        {
            Symbol symbol = new Symbol();
            Int32 character;
            Int32 count;
            Coder coder = new Coder(false, input, null, _compatabilityMode);

            while(true)
            {
                do
                {
                    _model.GetSymbolScale(symbol);
                    count = coder.GetCurrentCount(symbol);
                    character = _model.ConvertSymbolToInt(count, symbol);
                    coder.RemoveSymbol(symbol);
                }while(character == Constants.ESCAPE);
                
                if(character == Constants.DONE)
                {
                    break;
                }
                if(character != Constants.FLUSH)
                {
                    output.Write((byte)character);
                    CompressionTracker.Instance.IncrementOutput();
                }
                else
                {
                    _model.Flush();
                }
                _model.Update(character);
                _model.AddSymbol(character);
                _model.SetLastContext();
            }
        }

        public void ExpandSplit(string inputBaseName, BinaryWriter output)
        {
            Symbol symbol = new Symbol();
            Int32 character;
            Int32 count;
            Int32 part = 0;
            string currentInputName = String.Format("{0}-part{1}.bin", inputBaseName, part++);
            BinaryReader input = new BinaryReader(File.Open(currentInputName, FileMode.Open));
            
            _coder = new Coder(false, input, null, _compatabilityMode);

            while(true)
            {
                do
                {
                    _model.GetSymbolScale(symbol);
                    count = _coder.GetCurrentCount(symbol);
                    character = _model.ConvertSymbolToInt(count, symbol);
                    _coder.RemoveSymbol(symbol);
                } while (character == Constants.ESCAPE);

                if(character == Constants.DONE)
                {
                    break;
                }
                else if(character == Constants.EndOfPacket)
                {
                    input.Close();
                    currentInputName = String.Format("{0}-part{1}.bin", inputBaseName, part++);
                    input = new BinaryReader(File.Open(currentInputName, FileMode.Open));
                    _coder = new Coder(false, input, null, _compatabilityMode);
                }
                else if(character != Constants.FLUSH)
                {
                    output.Write((byte)character);
                    CompressionTracker.Instance.IncrementOutput();
                }
                else
                {
                    _model.Flush();
                }
                _model.Update(character);
                _model.AddSymbol(character);
            }
        }

        public async Task<bool> CompressPacketAsync(Queue<Int32> input, List<byte> output, Int32 packetSize, Int32 emptyBitsInLastByte = 0, bool padToSize = false)
        {
            bool result = false;
            var tcs = new TaskCompletionSource<bool>();
            Task completedTask;
            Thread? compressThread = null;
            CompressArgs? compressionArgs = null;

            EventHandler<CompressionEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                CompressionStatus -= handler;
                _packetCompleted = args.Complete;
                tcs.TrySetResult(true);
            };


            if(_packetInProgress)
            {
                _packetCompleted = false;
                CompressionStatus += handler;
                AddInput(input);
                //wait for result
                completedTask = await Task.WhenAny(tcs.Task);
                result = _packetCompleted;
                _packetInProgress = !result;
            }
            else
            {
                _packetCompleted = false;
                CompressionStatus += handler;
                //XXX FIXME Run in seperate thread
                compressionArgs = new CompressArgs(input, output, packetSize, emptyBitsInLastByte, padToSize);
                compressThread = new Thread(this.CompressPacketThread);
                compressThread.Start(compressionArgs);
                //CompressPacket(input, output, packetSize, emptyBitsInLastByte, padToSize);
                //wait for result
                completedTask = await Task.WhenAny(tcs.Task);
                result = _packetCompleted;
                _packetInProgress = !result;
            }

            return result;
        }

        private void CompressPacketThread(object? args)
        {
            CompressArgs? compArgs = args as CompressArgs;

            if(compArgs != null)
            {
                CompressPacket(compArgs.Input, compArgs.Output, compArgs.PacketSize, compArgs.EmptyBitsInLastByte, compArgs.PadToSize);
            }
        }

        private void CompressPacket(Queue<Int32> input, List<byte> output, Int32 packetSize, Int32 emptyBitsInLastByte = 0, bool padToSize = false)
        {
            Symbol symbol = new Symbol();
            bool done = false;
            CompressionEventArgs? compressionEventArgs = null;
            Int32 character = 0;
            Int16 textCount = 0;
            bool flush = false;
            bool escaped;
            List<Int32> inputList = new List<Int32>();
            List<byte> outputList = new List<byte>();
            Int64 sizeOfPacket = 0;
            _coder = new Coder(output, _compatabilityMode); 

            System.Threading.Monitor.Enter(_inputQue);
            foreach (Int32 data in input)
            {
                _inputQue.Enqueue(data);
            }

            while(!done)
            {

                if (!_static && (++textCount & 0x0ff) == 0)
                {
                    flush = CheckCompression();
                }

                if((_coder.OutputLength + Constants.NearEndPacketSize) > packetSize)
                {
                    if (!flush)
                    {
                        do
                        {
                            CompressionTracker.Instance.SetRollBackCheckPoint();
                            if (_inputQue.Count > 0)
                            {
                                character = _inputQue.Peek();
                                inputList.Add(character);
                                CompressionTracker.Instance.IncrementInput();
                            }
                            else
                            {
                                System.Threading.Monitor.Exit(_inputQue);
                                // trigger compression status event
                                compressionEventArgs = new CompressionEventArgs(false);
                                CompressionStatus?.Invoke(this, compressionEventArgs);
                                // wait for resume event
                                _inputAdded.WaitOne();
                                System.Threading.Monitor.Enter(_inputQue);
                                if(_inputQue.Count != 0)
                                {
                                    character = _inputQue.Peek();
                                    inputList.Add(character);
                                    CompressionTracker.Instance.IncrementInput();
                                }
                                else
                                {
                                    character = Constants.DONE;
                                    inputList.Add(character);
                                    done = true;
                                }
                            }

                            outputList.Clear();
                            if(!done)
                            {
                                inputList.Add(Constants.EndOfPacket);
                            }
                            else
                            {
                                inputList.Add(Constants.DONE);
                            }
                            _model.SetRollBackCheckPoint();
                            _coder.SetRollBackCheckPoint();
                            Compress(inputList, outputList, emptyBitsInLastByte);
                            inputList.RemoveAt(inputList.Count - 1);
                            _model.RollBack();
                            _coder.RollBack();
                            CompressionTracker.Instance.RollBack();
                            sizeOfPacket = _coder.OutputLength + outputList.Count;
                            if (sizeOfPacket <= packetSize)
                            {
                                _inputQue.Dequeue();
                            }
                        } while (sizeOfPacket <= packetSize && !done);
                        inputList.RemoveAt(inputList.Count - 1);
                        if (done)
                        {
                            inputList.Add(Constants.DONE);
                        }
                        else
                        {
                            inputList.Add(Constants.EndOfPacket);
                        }
                        outputList.Clear();
                        Compress(inputList, outputList);
                        foreach (byte bite in outputList)
                        {
                            output.Add(bite);
                        }

                        done = true;
                        _packetCompleted = true;
                    }
                }
                else
                {
                    if (_inputQue.Count > 0)
                    {
                        character = _inputQue.Dequeue();
                        CompressionTracker.Instance.IncrementInput();
                    }
                    else
                    {
                        System.Threading.Monitor.Exit(_inputQue);
                        // trigger compression status event
                        compressionEventArgs = new CompressionEventArgs(false);
                        CompressionStatus?.Invoke(this, compressionEventArgs);
                        // wait for resume event
                        _inputAdded.WaitOne();
                        System.Threading.Monitor.Enter(_inputQue);
                        if (_inputQue.Count != 0)
                        {
                            character = _inputQue.Dequeue();
                            CompressionTracker.Instance.IncrementInput();
                        }
                        else
                        {
                            character = Constants.DONE;
                        }
                    }

                    do
                    {
                        escaped = _model.ConvertIntToSymbol(character, symbol);
                        _coder.Encode(symbol);
                    } while (escaped);

                    if (character == Constants.FLUSH)
                    {
                        _model.Flush();
                        flush = false;
                    }
                    else if (character == Constants.DONE)
                    {
                        _coder.Flush(emptyBitsInLastByte, padToSize, packetSize);
                        break;
                    }
                    _model.Update(character);
                    _model.AddSymbol(character);
                }
            }
            System.Threading.Monitor.Exit(_inputQue);
            _packetCompleted = true;
            compressionEventArgs = new CompressionEventArgs(true);
            CompressionStatus?.Invoke(this, compressionEventArgs);
            while(padToSize && outputList.Count < packetSize)
            {
                output.Add(0x00);
            }
        }

        private void AddInput(Queue<Int32> additionalInput)
        {
            System.Threading.Monitor.Enter(_inputQue);
            foreach (Int32 input in additionalInput)
            {
                _inputQue.Enqueue(input);
            }
            System.Threading.Monitor.Exit(_inputQue);
            _inputAdded.Set();
        }

        private event EventHandler<CompressionEventArgs>? CompressionStatus;

        /*
        * This routine is called once every 256 input symbols.  Its job is to
        * check to see if the compression ratio falls below 10%.  If the
        * output size is 90% of the input size, it means not much compression
        * is taking place, so we probably ought to flush the statistics in the
        * model to allow for more current statistics to have greater impact.
        * This heuristic approach does seem to have some effect.
        */
        private bool CheckCompression()
        {
            bool result;
            UInt64 inputBytes;
            UInt64 outputBytes;
            UInt64 localRatio;

            inputBytes = CompressionTracker.Instance.InputBytes;
            outputBytes = CompressionTracker.Instance.OutputBytes;
            localRatio = (outputBytes * 100) / inputBytes;

            result = localRatio > Constants.CompressionLimit;
            CompressionTracker.Instance.Reset();

            return result;
        }

        public void ExportModel(Stream output)
        {
           _model.Export(output);
        }

        public UInt128 DictionaryStats(StreamWriter stream) => _model.DictionaryStats(stream);

        public UInt32 MaxOrder => _model.MaxOrder;

        private ModelOrderN _model;
        private Coder? _coder;
        private bool _compatabilityMode;
        private bool _static;
        private bool _packetInProgress = false;
        private bool _packetCompleted = true;

        private Queue<Int32> _inputQue = new Queue<Int32>();
        private AutoResetEvent _inputAdded = new AutoResetEvent(false);
    }
}