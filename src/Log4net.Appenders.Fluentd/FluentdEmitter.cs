//https://github.com/fluent/NLog.Targets.Fluentd

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MsgPack;
using MsgPack.Serialization;

namespace Log4net.Appenders.Fluentd
{
    internal class FluentdEmitter : IDisposable
    {
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly Packer _packer;
        private readonly SerializationContext _serializationContext;
        private readonly Stream _destination;


        public void Emit(DateTime timestamp, string tag, IDictionary<string, object> data)
        {
            long unixTimestamp = timestamp.ToUniversalTime().Subtract(_unixEpoch).Ticks / 10000000;
            _packer.PackArrayHeader(3);
            _packer.PackString(tag, Encoding.UTF8);
            _packer.Pack((ulong)unixTimestamp);
            _packer.Pack(data, _serializationContext);
            _destination.Flush();
        }


        public FluentdEmitter(Stream stream)
        {
            _destination = stream;
            _packer = Packer.Create(_destination);
            var embeddedContext = new SerializationContext(_packer.CompatibilityOptions);
            embeddedContext.Serializers.Register(new OrdinaryDictionarySerializer(embeddedContext, null));
            _serializationContext = new SerializationContext(PackerCompatibilityOptions.PackBinaryAsRaw);
            _serializationContext.Serializers.Register(new OrdinaryDictionarySerializer(_serializationContext, embeddedContext));
        }

        #region IDisposable Support
        private bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _destination?.Dispose();
                    _packer?.Dispose();
                }

                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
