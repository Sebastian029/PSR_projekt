//
// public class Startup
// {
//     // other startup code ...
//
//     public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//     {
//         if (env.IsDevelopment())
//         {
//             app.UseDeveloperExceptionPage();
//         }
//
//         app.UseRouting();
//
//         app.UseEndpoints(endpoints =>
//         {
//             endpoints.MapGrpcService<GreeterService>();
//
//             endpoints.MapGet("/", async context =>
//             {
//                 await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
//             });
//         });
//     }
// }