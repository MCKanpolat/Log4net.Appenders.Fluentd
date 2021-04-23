
# Log4net.Appenders.Fluentd
Fluentd appender for Log4net.
 
## Installation

    PM> Install-Package Log4net.Appenders.Fluentd
    or
    > dotnet add package Log4net.Appenders.Fluentd

##  Configuration

```xml
<appender name="Fluentd" type="Log4net.Appenders.Fluentd.FluentdAppender, Log4net.Appenders.Fluentd">
    <Host>127.0.0.1</Host>
    <Port>24224</Port>
    <Tag>YourTagHere</Tag>
    <NoDelay>false</NoDelay>
    <ReceiveBufferSize>8192</ReceiveBufferSize>
    <SendBufferSize>8192</SendBufferSize>
    <SendTimeout>1000</SendTimeout>
    <ReceiveTimeout>1000</ReceiveTimeout>
    <LingerEnabled>true</LingerEnabled>
    <LingerTime>1000</LingerTime>
    <EmitStackTraceWhenAvailable>true</EmitStackTraceWhenAvailable>
    <IncludeAllProperties>false</IncludeAllProperties>
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date{yyyy-MM-dd HH:mm:ss.fff} [%thread] [%property{Context}] %-5level %logger - %message%newline" />
    </layout>
</appender>
```
  

### Dependencies

    - log4net
    - MsgPack.Cli

### Building the project

    dotnet build

## Contribute

If you have any idea for an improvement or found a bug, do not hesitate to open an issue.


## License

Log4net.Appenders.Fluentd is distributed under MIT License.
