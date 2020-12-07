using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using log4net.Appender;
using log4net.Core;

namespace Log4net.Appenders.Fluentd
{
    public class FluentdAppender : AppenderSkeleton
    {
        public FluentdAppender()
        {
            Host = "127.0.0.1";
            Port = 24224;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            ReceiveTimeout = 1000;
            SendTimeout = 1000;
            LingerEnabled = true;
            LingerTime = 1000;
            EmitStackTraceWhenAvailable = false;
            Tag = Assembly.GetCallingAssembly().GetName().Name;
        }

        public string Host { get; set; }

        public int Port { get; set; }

        public string Tag { get; set; }

        public bool NoDelay { get; set; }

        public int ReceiveBufferSize { get; set; }

        public int SendBufferSize { get; set; }

        public int SendTimeout { get; set; }

        public int ReceiveTimeout { get; set; }

        public bool LingerEnabled { get; set; }

        public int LingerTime { get; set; }

        public bool EmitStackTraceWhenAvailable { get; set; }

        public bool IncludeAllProperties { get; set; }

        private TcpClient _client;

        private Stream _stream;

        private FluentdEmitter _emitter;


        public override void ActivateOptions()
        {
            base.ActivateOptions();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var renderedMessage = RenderLoggingEvent(loggingEvent);

            var record = new Dictionary<string, object> {
                { "level", loggingEvent.Level.Name },
                { "message", renderedMessage },
                { "logger_name", loggingEvent.LoggerName }
            };

            if (EmitStackTraceWhenAvailable && !string.IsNullOrWhiteSpace(loggingEvent.ExceptionObject?.StackTrace))
            {
                var transcodedFrames = new List<Dictionary<string, object>>();
                var stackTrace = new StackTrace(true);
                foreach (StackFrame frame in stackTrace.GetFrames())
                {
                    var transcodedFrame = new Dictionary<string, object>
                    {
                        { "filename", frame.GetFileName() },
                        { "line", frame.GetFileLineNumber() },
                        { "column", frame.GetFileColumnNumber() },
                        { "method", frame.GetMethod().ToString() },
                        { "il_offset", frame.GetILOffset() },
                        { "native_offset", frame.GetNativeOffset() },
                    };
                    transcodedFrames.Add(transcodedFrame);
                }
                record.Add("stacktrace", transcodedFrames);
            }

            if (IncludeAllProperties && loggingEvent.Properties.Count > 0)
            {
                foreach (var key in loggingEvent.Properties.GetKeys())
                {
                    var val = loggingEvent.Properties[key];
                    if (val == null)
                        continue;

                    record.Add(key, SerializePropertyValue(key, val));
                }
            }

            try
            {
                EnsureConnected();
            }
            catch (Exception ex)
            {
                base.ErrorHandler.Error($"{nameof(FluentdAppender)} EnsureConnected - {ex.Message}");
            }

            try
            {
                _emitter?.Emit(loggingEvent.TimeStamp, Tag, record);
            }
            catch (Exception ex)
            {
                base.ErrorHandler.Error($"{nameof(FluentdAppender)} Emit - {ex.Message}");
            }
        }

        protected void EnsureConnected()
        {
            if (_client == null)
            {
                InitializeClient();
                ConnectClient();
            }
            else if (!_client.Connected)
            {
                Cleanup();
                InitializeClient();
                ConnectClient();
            }
        }

        private void InitializeClient()
        {
            _client = new TcpClient
            {
                NoDelay = NoDelay,
                ReceiveBufferSize = ReceiveBufferSize,
                SendBufferSize = SendBufferSize,
                SendTimeout = SendTimeout,
                ReceiveTimeout = ReceiveTimeout,
                LingerState = new LingerOption(LingerEnabled, LingerTime)
            };
        }

        private void ConnectClient()
        {
            _client.Connect(Host, Port);
            _stream = _client.GetStream();
            _emitter = new FluentdEmitter(_stream);
        }

        protected void Cleanup()
        {
            try
            {
                _stream?.Dispose();
                _client?.Close();
            }
            catch (Exception ex)
            {
                base.ErrorHandler.Error($"{nameof(FluentdAppender)} Cleanup - {ex.Message}");
            }
            finally
            {
                _stream = null;
                _client = null;
                _emitter = null;
            }
        }

        private static object SerializePropertyValue(string propertyKey, object propertyValue)
        {
            if (propertyValue == null || Convert.GetTypeCode(propertyValue) != TypeCode.Object || propertyValue is decimal)
            {
                return propertyValue;   // immutable
            }
            else
            {
                return propertyValue.ToString();
            }
        }
    }
}
