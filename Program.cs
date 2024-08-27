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

// �������������� � ������� ����
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorization();
builder.Services.AddTransient<ICalculateService, Calculator>();//������� � ��������� �������� ������ ICalculateService

var app = builder.Build();

app.UseAuthentication();//middleware ��������������
app.UseAuthorization();//middleware �����������


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
    // ���� email �/��� ������ �� �����������, �������� ��������� ��� ������ 400
    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
        return Results.BadRequest("Email �/��� ������ �� �����������");

    string email = form["email"];
    string password = form["password"];

    // ������� ������������ 
    User? user = users.FirstOrDefault(p => p.Email == email && p.Password == password);
    // ���� ������������ �� ������, ���������� ��������� ��� 401
    if (user is null)
    
      
        return Results.Content($"User: {email}, {password}\nNot Found");//.Unauthorized();
    
   
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Email) };
        // ������� ������ ClaimsIdentity
        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");

        // ��������� ������������������ ����
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));//������������������ ����
        return Results.Redirect(returnUrl ?? "/");//�������������� ���������������������� ������������ ������� �� �����, � �������� ��� ����������� �� ����� ������
    


    

});

app.MapGet("/logout", async (HttpContext context) =>//�������� ����� ��� ������ � �������� ������������������ ����
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);//������� ������������������ ����
    return Results.Redirect("/login");
});

//������ � ��� ����� ������ ������������������� ������������
//GET-������� ������ ���� ��������������, �.� ��������� ������ �� ������ �������� ��������� �������. ��� ������ ������������ ��� ��������� ����������.
app.MapGet("/", [Authorize] async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

//POST-������� ����� �������� ��������� �������, ��������, �������� ����� ������ ��� ������� ������������.
app.MapPost("/api/calculate", [Authorize] async (HttpContext context) =>
{

    var message = "������������ ������";   // ���������� ��������� �� ���������
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

    // ���������� ������������ ������
    await context.Response.WriteAsJsonAsync(new { text = message });//new ��� �������� ������ ���������� ���������� ���� � ����� ��������� text, �������� �������� ����� ���������� message. ����� ���� ��������� ����� ������������ � JSON.

});

app.Run();


public record Expression(string Content);