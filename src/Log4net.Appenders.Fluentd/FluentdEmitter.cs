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
            ulong unixTimestamp = Convert.ToUInt64(Math.Floor((decimal)timestamp.ToUniversalTime().Subtract(_unixEpoch).Ticks / (decimal)10000000));

            ulong extendedTime = (((ulong)unixTimestamp & 0x00000000FFFFFFFF) << 32) | (((ulong)timestamp.Millisecond * 1000000) & 0x00000000FFFFFFFF);
            extendedTime = (extendedTime & 0x00000000FFFFFFFF) << 32 | (extendedTime & 0xFFFFFFFF00000000) >> 32;
            extendedTime = (extendedTime & 0x0000FFFF0000FFFF) << 16 | (extendedTime & 0xFFFF0000FFFF0000) >> 16;
            extendedTime = (extendedTime & 0x00FF00FF00FF00FF) << 8 |  (extendedTime & 0xFF00FF00FF00FF00) >> 8;

            _packer.PackArrayHeader(3);
            _packer.PackString(tag, Encoding.UTF8);
            _packer.PackExtendedTypeValue(0,BitConverter.GetBytes(extendedTime));
            _packer.Pack(data, _serializationContext);
            _destination.Flush();
        }


        public FluentdEmitter(Stream stream)
        {
            _destination = stream;
            _packer = Packer.Create(_destination, PackerCompatibilityOptions.None);
            var embeddedContext = new SerializationContext(_packer.CompatibilityOptions);
            embeddedContext.Serializers.Register(new OrdinaryDictionarySerializer(embeddedContext, null));
            _serializationContext = new SerializationContext(PackerCompatibilityOptions.None);
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
