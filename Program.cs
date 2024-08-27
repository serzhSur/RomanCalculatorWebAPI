using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using RomanCalculatorWeb.models;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var users = new List<User>
{
    new User{ Email="guest", Password="123" }
};

// аутентификация с помощью куки
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorization();
builder.Services.AddTransient<ICalculateService, Calculator>();//добавим в коллекцию сервисов сервис ICalculateService

var app = builder.Build();

app.UseAuthentication();//middleware аутентификации
app.UseAuthorization();//middleware авторизации


app.MapGet("/login", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/login.html");
});

app.MapPost("/login", async (string? returnUrl, HttpContext context) =>
{
    //try { }
    //catch(Exception ex) { }
    var form = context.Request.Form;
    // если email и/или пароль не установлены, посылаем статусный код ошибки 400
    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
        return Results.BadRequest("Email и/или пароль не установлены");

    string email = form["email"];
    string password = form["password"];

    // находим пользователя 
    User? user = users.FirstOrDefault(p => p.Email == email && p.Password == password);
    // если пользователь не найден, отправляем статусный код 401
    if (user is null)
    
      
        return Results.Content($"User: {email}, {password}\nNot Found");//.Unauthorized();
    
   
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Email) };
        // создаем объект ClaimsIdentity
        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");

        // установка аутентификационных куки
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));//аутентификационные куки
        return Results.Redirect(returnUrl ?? "/");//перенаправляем аутентификацированного пользователя обратно на адрес, с которого его перебросило на форму логина
    


    

});

app.MapGet("/logout", async (HttpContext context) =>//конечная точка для выхода и удаления аутентификационные куки
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);//удаляет аутентификационные куки
    return Results.Redirect("/login");
});

//доступ к ней имеют только аутентифицированные пользователи
//GET-запросы должны быть идемпотентными, т.е повторный запрос не должен изменять состояние сервера. Они обычно используются для получения информации.
app.MapGet("/", [Authorize] async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

//POST-запросы могут изменять состояние сервера, например, добавляя новые данные или изменяя существующие.
app.MapPost("/api/calculate", [Authorize] async (HttpContext context) =>
{

    var message = "Некорректные данные";   // содержание сообщения по умолчанию
    try
    {
        var expression = await context.Request.ReadFromJsonAsync<Expression>();
        if (expression != null)
        {
            var calculateService = app.Services.GetService<ICalculateService>();
            message = calculateService.Calculate(expression.Content);
            message += $"\n{calculateService.Errors}";
        }
    }
    catch (Exception ex)
    {
        //log
    }

    // отправляем пользователю данные
    await context.Response.WriteAsJsonAsync(new { text = message });//new для создания нового экземпляра анонимного типа с одним свойством text, значение которого равно переменной message. Затем этот экземпляр будет сериализован в JSON.

});

app.Run();


public record Expression(string Content);