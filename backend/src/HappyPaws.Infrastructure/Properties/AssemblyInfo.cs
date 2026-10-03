using System.Runtime.CompilerServices;

// The model adapters are internal; their request and response mapping is tested directly against
// a scripted HTTP handler, without standing up a model.
[assembly: InternalsVisibleTo("HappyPaws.IntegrationTests")]
