using System.Runtime.CompilerServices;

// The model adapters and the SMS and email senders are internal; they are tested directly against
// scripted HTTP handlers and recording transports, without a model, a gateway or a mail server.
[assembly: InternalsVisibleTo("HappyPaws.IntegrationTests")]
