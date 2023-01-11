using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalAPINET6.Logic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = "http://localhost",
        ValidAudience = "http://localhost/audience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("0938ccf0-afee-410b-b460-6d23c6f1570e")),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

List<Movie> movies = new()
{
    new() { Id = 1, Rating = 5, Title = "Shrek" },
    new() { Id = 2, Rating = 1, Title = "Inception" },
    new() { Id = 3, Rating = 3, Title = "Jaws" },
    new() { Id = 4, Rating = 1, Title = "The Green Latern" },
    new() { Id = 5, Rating = 5, Title = "The Matrix" },
};

app.MapGet("/api/movies/", () =>
{
    return Results.Ok(movies);
}).RequireAuthorization();

app.MapGet("/api/movies/{id:int}", (int id) =>
{
    return Results.Ok(movies.Single(x => x.Id == id));
});

app.MapPost("/api/movies/", (Movie movie) =>
{
    movies.Add(movie);

    return Results.Ok(movies);
});

app.MapDelete("/api/movies/{id:int}", (int id) =>
{
    movies.Remove(movies.Single(x => x.Id == id));

    return movies;
});

app.MapPost("api/login", [AllowAnonymous] (User user) =>
{
    if (user.UserName != "klc" && user.Password != "klc1234")
        return Results.Unauthorized();

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
            {
                new Claim("username", user.UserName),
            }),
        Expires = DateTime.UtcNow.AddMinutes(5),
        Issuer = "http://localhost",
        Audience = "http://localhost/audience",
        SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes("0938ccf0-afee-410b-b460-6d23c6f1570e")),
                    SecurityAlgorithms.HmacSha512Signature)
    };
    JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
    SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

    string generatedToken = tokenHandler.WriteToken(token);

    return Results.Ok(generatedToken);
});

app.Run();