using Avro;
using Avro.Specific;
using System.Collections.Concurrent;


namespace ShellSquare.Client.ETP
{
    public class EtpSpecificReader : SpecificDefaultReader
    {
        private static readonly ConcurrentDictionary<string, string> Namespaces = new ConcurrentDictionary<string, string>();
        private const string RootNamespace = "Energistics.";

        public EtpSpecificReader(Schema writerSchema, Schema readerSchema) : base(writerSchema, readerSchema)
        {
        }

        protected override object ReadRecord(object reuse, RecordSchema writerSchema, Schema readerSchema, Avro.IO.Decoder decoder)
        {
            reuse = reuse ?? CreateInstance(readerSchema, Schema.Type.Record);
            return base.ReadRecord(reuse, writerSchema, readerSchema, decoder);
        }

        protected override object ReadFixed(object reuse, FixedSchema writerSchema, Schema readerSchema, Avro.IO.Decoder decoder)
        {
            reuse = reuse ?? CreateInstance(readerSchema, Schema.Type.Fixed);
            return base.ReadFixed(reuse, writerSchema, readerSchema, decoder);
        }

        protected override object ReadArray(object reuse, ArraySchema writerSchema, Schema readerSchema, Avro.IO.Decoder decoder)
        {
            var arraySchema = readerSchema as ArraySchema;
            reuse = reuse ?? CreateInstance(arraySchema?.ItemSchema, Schema.Type.Array);
            return base.ReadArray(reuse, writerSchema, readerSchema, decoder);
        }

        protected override object ReadMap(object reuse, MapSchema writerSchema, Schema readerSchema, Avro.IO.Decoder decoder)
        {
            var mapSchema = readerSchema as MapSchema;
            reuse = reuse ?? CreateInstance(mapSchema?.ValueSchema, Schema.Type.Map);
            return base.ReadMap(reuse, writerSchema, readerSchema, decoder);
        }

        private object CreateInstance(Schema schema, Schema.Type schemaType)
        {
            var recordSchema = schema as RecordSchema;
            var fixedSchema = schema as FixedSchema;
            object instance = null;

            if (!string.IsNullOrWhiteSpace(recordSchema?.Fullname))
            {
                // var typeName = GetTypeName(recordSchema.Fullname);
                var typeName = recordSchema.Fullname;
                instance = ObjectCreator.Instance.New(typeName, schemaType);
            }
            else if (!string.IsNullOrWhiteSpace(fixedSchema?.Fullname))
            {
                var typeName = GetTypeName(fixedSchema.Fullname);
                instance = ObjectCreator.Instance.New(typeName, schemaType);
            }

            return instance;
        }

        private string GetTypeName(string typeName)
        {
            return Namespaces.GetOrAdd(typeName, key =>
            {
                var result = key;

                // Map legacy namespaces to Etp.v11 namespace
                if (result.StartsWith(RootNamespace) && !result.StartsWith($"{RootNamespace}Etp."))
                    result = $"{RootNamespace}Etp.v11.{result.Substring(RootNamespace.Length)}";

                return result;
            });
        }
    }
}
