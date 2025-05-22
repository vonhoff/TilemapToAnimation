using System.CommandLine;

namespace Tilemap2Animation.CommandLineOptions.Contracts;

public interface ICommandLineOption<T>
{
    Option<T> Option { get; }

    Option<T> Register(Command command);
}