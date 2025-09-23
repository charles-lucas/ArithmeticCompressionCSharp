using System.Text.Json;

namespace ArithmeticCoder
{
    public class ArithmeticCompression
    {
        //Ctor for loading a model from JSON
        public ArithmeticCompression(Stream modelStream)
        {
            StreamReader inputReader = new StreamReader(modelStream);
            string? json;
            ModelOrderN? model;
            ContextKey? contextKey;
            Context? context;
            bool done = false;
            
            _model = new ModelOrderN(0);

            json = inputReader.ReadLine();
            if(json != null)
            {
                model = JsonSerializer.Deserialize<ModelOrderN>(json);
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
        }

        public ArithmeticCompression(UInt32 maxOrder, bool compatabilityMode = false)
        {
            _model = new ModelOrderN(maxOrder, compatabilityMode);
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
            _coder = new Coder(true, input, output);

            while (true)
            {
                if ((++textCount & 0x0ff) == 0)
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

                _model.Update((byte)character);
                _model.AddSymbol(character);
                //_model.Print((byte)character);
            }
            _coder.Flush();
        }

        public void Expand(BinaryReader input, BinaryWriter output)
        {
            Symbol symbol = new Symbol();
            Int32 character;
            Int32 count;
            Coder coder = new Coder(false, input, null);

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

        public void LoadModel(System.IO.BinaryReader reader)
        {
            Symbol symbol = new Symbol();
            Coder coder = new Coder(false, reader, null);
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

        public void ExportModel(Stream output)
        {
           _model.Export(output);
        }

        public UInt128 DictionaryStats(StreamWriter stream) => _model.DictionaryStats(stream);

        public UInt32 MaxOrder => _model.MaxOrder;

        public string Context => _model.LastContext.ToString();

        private ModelOrderN _model;
        private Coder? _coder;

        Int64 _localInputMarker;
        Int64 _localOutputMarker;
    }
}