using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Bookings.Infrastructure.Data;

internal sealed class BookingsDbContextFactory : IDesignTimeDbContextFactory<BookingsDbContext>
{
    public BookingsDbContext CreateDbContext(string[] args)
    {
        var infrastructurePath = Directory.GetCurrentDirectory();
        var solutionPath = Directory.GetParent(infrastructurePath)?.FullName;

        if (string.IsNullOrEmpty(solutionPath))
        {
            throw new DirectoryNotFoundException("Не удалось определить корневую директорию решения.");
        }

        var presentationPath = Directory.GetDirectories(solutionPath)
            .FirstOrDefault(dir =>
                File.Exists(Path.Combine(dir, "appsettings.json")) &&
                File.Exists(Path.Combine(dir, "Program.cs")));

        if (presentationPath == null)
        {
            throw new FileNotFoundException("Не удалось найти проект слоя Presentation (с файлами appsettings.json и Program.cs).");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(presentationPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена.");
        }

        var builder = new DbContextOptionsBuilder<BookingsDbContext>();
        builder.UseNpgsql(connectionString);

        return new BookingsDbContext(builder.Options);
    }
}
