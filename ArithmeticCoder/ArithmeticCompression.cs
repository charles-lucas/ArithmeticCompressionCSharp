using System.Net.Http.Headers;
using System.Text.Json;

namespace ArithmeticCoder
{
    public class CompressionEventArgs : EventArgs
    {
        public CompressionEventArgs(bool complete)
        {
            _complete = complete;
        }

        public bool Complete => _complete;

        private readonly bool _complete;
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

        public ArithmeticCompression(ModelOrderN model)
        {
            _model = model;
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
                    flush = CheckCompression(input, output);
                }

                if (!flush)
                {
                    try
                    {
                        character = input.ReadByte();
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

        public void CompressSplit(BinaryReader input, string outputBaseName, Int32 partMax)
        {
            Int32 character;
            Symbol symbol = new Symbol();
            bool escaped;
            bool flush = false;
            Int16 textCount = 0;
            Int32 parts = 0;
            Int32 bitsLeftInRack = 0;
            string outputFileName = String.Format("{0}-part{1}.bin", outputBaseName, parts++);
            BinaryWriter output = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
            Int32 extraByteForOverFLow;

            _coder = new Coder(true, input, output, _compatabilityMode);

            while (true)
            {
                if (!_static && (++textCount & 0x0ff) == 0)
                {
                    flush = CheckCompression(input, output);
                }

                if (!flush)
                {
                    if (_coder.UnderflowBits > 0)
                    {
                        for(byte bite = _coder.Mask; bite > 0;  bite >>= 1)
                        {
                            bitsLeftInRack++;
                        }
                        extraByteForOverFLow = (Int32)_coder.UnderflowBits - bitsLeftInRack;
                        if(extraByteForOverFLow > 0)
                        {
                            extraByteForOverFLow = extraByteForOverFLow / 8 + 2;
                        }
                        else
                        {
                            extraByteForOverFLow = 1;
                        }
                    }
                    else if(_coder.Mask != 0x80)
                    {
                        extraByteForOverFLow = 1;
                    }
                    else
                    {
                        extraByteForOverFLow = 0;
                    }

                    if ((_coder.OutputLength + extraByteForOverFLow + Constants.EndOfPacketSpace) > partMax)
                    {
                        character = Constants.EndOfPacket;
                    }
                    else
                    {
                        try
                        {
                            character = input.ReadByte();
                        }
                        catch (EndOfStreamException)
                        {
                            _model.SetLastContext();
                            character = Constants.DONE;
                        }
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
                else if (character == Constants.EndOfPacket)
                {
                    _coder.Flush(partMax);
                    output.Flush();
                    output.Close();
                    outputFileName = String.Format("{0}-part{1}.bin", outputBaseName, parts++);
                    output = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
                    _coder = new Coder(true, input, output, _compatabilityMode);
                }
                else if (character == Constants.DONE)
                {
                    break;
                }
                
                _model.Update(character);
                _model.AddSymbol(character);
            }
            _coder.Flush(partMax);
            output.Flush();
            output.Close();
        }

        public void CompressSplitRollBack(BinaryReader input, string outputBaseName, Int32 partMax, bool pad = false)
        {
            Int32 character;
            Symbol symbol = new Symbol();
            bool escaped;
            bool flush = false;
            Int16 textCount = 0;
            Int32 parts = 0;
            string outputFileName = String.Format("{0}-part{1}.bin", outputBaseName, parts++);
            BinaryWriter output = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
            List<Int32> inputList = new List<Int32>();
            List<byte> outputList = new List<byte>();
            Queue<Int32> leftOverInput = new Queue<Int32>();
            bool finalData = false;
            Int32 index = 0;
            Int64 sizeOfPacket = 0;

            _coder = new Coder(true, input, output, _compatabilityMode);

            while (true)
            {
                if (!_static && (++textCount & 0x0ff) == 0)
                {
                    flush = CheckCompression(input, output);
                }

                if (!flush)
                {
                    //if we are near the end of the packet start running trials to see how much we can put in
                    if (!finalData && (_coder.OutputLength + Constants.NearEndPacketSize) > partMax)
                    {
                        do
                        {
                            try
                            {
                                character = input.ReadByte();
                                inputList.Add(character);
                            }
                            catch (EndOfStreamException)
                            {
                                _model.SetLastContext();
                                character = Constants.DONE;
                                inputList.Add(character);
                            }
                            outputList.Clear();
                            inputList.Add(Constants.EndOfPacket);
                            _model.SetRollBackCheckPoint();
                            _coder.SetRollBackCheckPoint();
                            Compress(inputList, outputList);
                            inputList.RemoveAt(inputList.Count - 1);
                            _model.RollBack();
                            _coder.RollBack();
                            sizeOfPacket = _coder.OutputLength + outputList.Count;
                        } while (sizeOfPacket <= partMax);
                        finalData = true;
                        index = 0;
                        leftOverInput.Enqueue(inputList[inputList.Count - 1]);
                        inputList.RemoveAt(inputList.Count - 1);
                        inputList.Add(Constants.EndOfPacket);
                        character = inputList[index++];
                    }
                    else
                    {
                        if ((!finalData) && leftOverInput.Count == 0)
                        {
                            try
                            {
                                character = input.ReadByte();
                            }
                            catch (EndOfStreamException)
                            {
                                _model.SetLastContext();
                                character = Constants.DONE;
                            }
                        }
                        else if((!finalData) && leftOverInput.Count > 0)
                        {
                            character = leftOverInput.Dequeue();
                        }
                        else
                        {
                            character = inputList[index++];
                        }
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
                else if (character == Constants.EndOfPacket)
                {
                    _coder.Flush(partMax, pad);
                    output.Flush();
                    output.Close();
                    outputFileName = String.Format("{0}-part{1}.bin", outputBaseName, parts++);
                    output = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
                    _coder = new Coder(true, input, output, _compatabilityMode);
                    finalData = false;
                    inputList.Clear();
                }
                else if (character == Constants.DONE)
                {
                    break;
                }
                _model.Update(character);
                _model.AddSymbol(character);
            }
            _coder.Flush(partMax, pad);
            output.Flush();
            output.Close();
        }

        public bool CompressSplit(Queue<Int32> input,  List<byte> output, Int32 partMax, Int32 emptyBitsInLastByte)
        {
            Int32 character;
            Symbol symbol = new Symbol();
            bool escaped;
            Queue<Int32> leftOverInput = new Queue<Int32>();
            Int64 sizeOfPacket = 0;
            bool result = false;
            List<Int32> inputList = new List<Int32>();
            List<Byte> outputList = new List<Byte>();
            bool flush = false;
            Int16 textCount = 0;

            _coder = new Coder(true, null, null, _compatabilityMode);

            while (true)
            {
                //if we are near the end of the packet start running trials to see how much we can put in
                if ((_coder.OutputLength + Constants.NearEndPacketSize) > partMax)
                {

                    if (!_static && (++textCount & 0x0ff) == 0)
                    {
                        flush = CheckCompression(input, output);
                    }

                    if (!flush)
                    {
                        do
                        {

                            if(input.Count > 0)
                            {
                                character = input.Peek();
                                inputList.Add(character);
                            }
                            else
                            {
                                return false;
                            }

                            outputList.Clear();
                            inputList.Add(Constants.EndOfPacket);
                            _model.SetRollBackCheckPoint();
                            _coder.SetRollBackCheckPoint();
                            Compress(inputList, outputList);
                            inputList.RemoveAt(inputList.Count - 1);
                            _model.RollBack();
                            _coder.RollBack();
                            sizeOfPacket = _coder.OutputLength + outputList.Count;
                            if (sizeOfPacket <= partMax)
                            {
                                input.Dequeue();
                            }
                        } while (sizeOfPacket <= partMax);
                        inputList.RemoveAt(inputList.Count - 1);
                        inputList.Add(Constants.EndOfPacket);
                        Compress(inputList, outputList);
                        foreach (byte bite in outputList)
                        {
                            output.Add(bite);
                        }
                        result = true;
                    }
                }
                else
                {
                    if(input.Count > 0)
                    {
                        character = input.Dequeue();
                    }
                    else
                    {
                        return false;
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
            }

            return result;
        }

        public void Compress(List<Int32> input, List<byte> output)
        {
            Int32 character;
            Symbol symbol = new Symbol();
            bool escaped;
            bool flush = false;
            Int32 index = 0;

            while (index < input.Count)
            {
                if (!flush)
                {
                    character = input[index++];
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
            _coder?.Flush(output);
        }

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
                }
                else
                {
                    _model.Flush();
                }
                _model.Update(character);
                _model.AddSymbol(character);
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
                }
                else
                {
                    _model.Flush();
                }
                _model.Update(character);
                _model.AddSymbol(character);
            }
        }

        public List<byte> CompressPacket(Queue<Int32> input, Int32 packetSize, Int32 paddingBits)
        {
            List<byte> result = new List<byte>();
            bool done = false;
            CompressionEventArgs? compressionEventArgs = null;
            System.Threading.Monitor.Enter(_inputQue);
            foreach (Int32 data in input)
            {
                _inputQue.Enqueue(data);
            }

            while(!done)
            {
                if (_inputQue.Count == 0)
                {
                    System.Threading.Monitor.Exit(_inputQue);
                    // trigger compression status event
                    compressionEventArgs = new CompressionEventArgs(false);
                    CompressionStatus?.Invoke(this, compressionEventArgs);
                    // wait for resume event
                    _inputAdded.WaitOne();
                    System.Threading.Monitor.Enter(_inputQue);
                }

                CompressSplit(input, result, packetSize, paddingBits);

            }

            System.Threading.Monitor.Exit(_inputQue);


            return result;
        }

        public void AddInput(List<Int32> additionalInput)
        {
            System.Threading.Monitor.Enter(_inputQue);
            foreach (Int32 input in additionalInput)
            {
                _inputQue.Enqueue(input);
            }
            System.Threading.Monitor.Exit(_inputQue);
            _inputAdded.Set();
        }

        public void LoadModel(System.IO.BinaryReader reader)
        {
            Symbol symbol = new Symbol();
            Coder coder = new Coder(false, reader, null, _compatabilityMode);
            Int32 character;
            Int32 count;

            while(true)
            {
                do
                {
                    _model.GetSymbolScale(symbol);
                    count = coder.GetCurrentCount(symbol);
                    character = _model.ConvertSymbolToInt(symbol);
                    coder.RemoveSymbol(symbol);
                }while(character == Constants.ESCAPE);

                if(character == Constants.DONE)
                {
                    break;
                }

                if(character == Constants.FLUSH)
                {
                    _model.Flush();
                }
                
                _model.Update(character);
                _model.AddSymbol(character);
            }
        }

        public void Write(byte[] bites)
        {

        }

        public void Write(byte bite)
        {

        }

        public byte ReadByte()
        {
            return 0x00;
        }

        public event EventHandler<CompressionEventArgs>? CompressionStatus;

        protected void OnCompressionStatus(CompressionEventArgs ea)
        {
            CompressionStatus?.Invoke(this, ea);
        }

        private bool CheckCompression(BinaryReader input, BinaryWriter output)
        {
            bool result = true;
            Int64 totalInputBytes;
            Int64 totalOutputBytes;
            Int64 localRatio;

            totalInputBytes = input.BaseStream.Position - _localInputMarker;
            totalOutputBytes = output.BaseStream.Position - _localOutputMarker;

            if (totalOutputBytes == 0)
            {
                totalOutputBytes = 1;
            }

            localRatio = (totalOutputBytes * 100) / totalInputBytes;
            _localInputMarker = input.BaseStream.Position;
            _localOutputMarker = output.BaseStream.Position;

            result = localRatio > Constants.CompressionLimit;

            return result;
        }

        private bool CheckCompression(List<Int32> input, List<byte> output)
        {
            bool result = false;

            //XXX FIXME need to think how this works when using lists...

            return result;
        }

        private bool CheckCompression(Queue<Int32> input, List<byte> output)
        {
            bool result = false;

            //XXX FIXME need to think how this works when using lists...

            return result;
        }

        public void ExportModel(Stream output)
        {
           _model.Export(output);
        }

        public UInt128 DictionaryStats(StreamWriter stream) => _model.DictionaryStats(stream);

        public UInt32 MaxOrder => _model.MaxOrder;

        public string Context => _model.LastContext.ToString();

        private ModelOrderN _model;
        private Coder? _coder;
        private bool _compatabilityMode;
        private bool _static;

        private Queue<Int32>? _inputQue;
        private AutoResetEvent _inputAdded = new AutoResetEvent(false);

        Int64 _localInputMarker;
        Int64 _localOutputMarker;
    }
}