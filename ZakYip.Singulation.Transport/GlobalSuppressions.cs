// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// Transport layer must be resilient - network errors should not crash the system
// Event raising methods must catch all exceptions to prevent breaking subscribers

// Event raising methods - must not throw
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport.TouchClientByteTransport.RaiseData(System.ReadOnlyMemory{System.Byte})")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport.TouchClientByteTransport.RaiseBytesReceived(System.Byte[],System.Int32)")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport.TouchClientByteTransport.RaiseState(ZakYip.Singulation.Core.Enums.TransportConnectionState,System.String,System.String,System.Int32,System.Nullable{System.TimeSpan},System.Boolean)")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport.TouchClientByteTransport.RaiseError(System.String,System.Exception,System.Boolean,System.String,System.Int32)")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport.TouchServerByteTransport.RaiseData(System.ReadOnlyMemory{System.Byte})")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport.TouchServerByteTransport.RaiseBytesReceived(System.Byte[],System.Int32)")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport.TouchServerByteTransport.RaiseState(ZakYip.Singulation.Core.Enums.TransportConnectionState,System.String,System.String,System.Int32,System.Nullable{System.TimeSpan},System.Boolean)")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event raising must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport.TouchServerByteTransport.RaiseError(System.String,System.Exception,System.Boolean,System.String,System.Int32)")]

// Cleanup methods - must not throw to avoid masking original exceptions
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Cleanup code must not throw to avoid masking original exceptions",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport.TouchClientByteTransport.CloseAndDisposeAsync(TouchSocket.Sockets.TcpClient)~System.Threading.Tasks.Task")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Cleanup code must not throw to avoid masking original exceptions",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpClientByteTransport.TouchClientByteTransport.StopAsync(System.Threading.CancellationToken)~System.Threading.Tasks.Task")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Cleanup code must not throw to avoid masking original exceptions",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Transport.Tcp.TcpServerByteTransport.TouchServerByteTransport.StopAsync(System.Threading.CancellationToken)~System.Threading.Tasks.Task")]
