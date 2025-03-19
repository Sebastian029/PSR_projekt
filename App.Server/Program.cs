
namespace App.Server
{
    public class Program
    {
        //public static void Main(string[] args)
        //{
        //    var builder = WebApplication.CreateBuilder(args);

        //    // Add services to the container.

        //    builder.Services.AddControllers();
        //    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        //    builder.Services.AddEndpointsApiExplorer();
        //    builder.Services.AddSwaggerGen();

        //    var app = builder.Build();

        //    app.UseDefaultFiles();
        //    app.UseStaticFiles();

        //    // Configure the HTTP request pipeline.
        //    if (app.Environment.IsDevelopment())
        //    {
        //        app.UseSwagger();
        //        app.UseSwaggerUI();
        //    }

        //    app.UseHttpsRedirection();

        //    app.UseAuthorization();


        //    app.MapControllers();

        //    app.MapFallbackToFile("/index.html");

        //    app.Run();
        //}
        static void Main()
        {
            var game = new Checkers();
            game.PrintBoard();

            while (true)
            {
                Console.Write("Enter move (fromY fromX toY toX): ");
                var input = Console.ReadLine();
                var parts = input.Split();
                if (parts.Length != 4) continue;

                if (int.TryParse(parts[0], out int fromX) &&
                    int.TryParse(parts[1], out int fromY) &&
                    int.TryParse(parts[2], out int toX) &&
                    int.TryParse(parts[3], out int toY))
                {
                    if (game.MovePiece(fromX, fromY, toX, toY))
                        game.PrintBoard();
                }
            }
        }
    }
}
