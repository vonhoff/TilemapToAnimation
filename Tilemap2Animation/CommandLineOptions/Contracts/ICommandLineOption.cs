using System.CommandLine;

namespace Tilemap2Animation.CommandLineOptions.Contracts;

/// <summary>
/// Interface for command line options
/// </summary>
/// <typeparam name="T">The type of the option value</typeparam>
public interface ICommandLineOption<T>
{
    /// <summary>
    /// Gets the option object
    /// </summary>
    Option<T> Option { get; }
} 