using PlataAlfa.core;
using PlataAlfa.data.V1_0.Admin;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace PlataAlfa.api.V1_0
{
    public class Auth : Entity
    {
        string plainTextSecurityKey = "abcdefghijklmnopqrstuvwyxz01234567980";

        public Envelope<dynamic> Login(dynamic data, UsuariosDS usuariosDS)
        {
            string usuario = data.Usuario;
            string password = data.Password;
            var response = usuariosDS.GetByUsuario(new { Usuario = usuario.ToLower() });
            
            if (response.Result == "empty")
            {
                return new Envelope<dynamic>() { Result = "notSuccess", Message = "Usuario o Password no encontrado" };
            }
            var dataSet = response.Data.FirstOrDefault();
            if (HashHL.SHA256Of($"{usuario}{password}{dataSet.PasswordSalt}") != dataSet.Password &&
                dataSet.Password != password)
            {
                return new Envelope<dynamic>() { Result = "notSuccess", Message = "Usuario o Password no encontrado" };
            }
            else
            {
                var signingKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(plainTextSecurityKey));
                var signingCredentials = new SigningCredentials(signingKey,
                    SecurityAlgorithms.HmacSha256Signature);

                var x = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario),
                    new Claim(ClaimTypes.Name, dataSet.Nombre ),
                    new Claim(ClaimTypes.Surname, dataSet.Apellidos )
                };

                var claimsIdentity = new ClaimsIdentity(x, "Custom");

                var securityTokenDescriptor = new SecurityTokenDescriptor()
                {
                    Audience = "http://localhost:61101",
                    Issuer = "http://localhost:61101",
                    Subject = claimsIdentity,
                    Expires = DateTime.Now.AddHours(12),
                    SigningCredentials = signingCredentials,
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var plainToken = tokenHandler.CreateToken(securityTokenDescriptor);
                var signedAndEncodedToken = tokenHandler.WriteToken(plainToken);

                dynamic dataReturn = new { Token = signedAndEncodedToken,
                    Usuario = usuario,
                    dataSet.Nombre,
                    dataSet.Apellidos,
                    dataSet.Email
                };
                return new Envelope<dynamic>() { Result = "ok", Data = dataReturn };
            }

        }

        public Envelope TokenValid(dynamic data)
        {
            try
            {
                string signedAndEncodedToken = data.Token;
                var tokenHandler = new JwtSecurityTokenHandler();
                var plainTextSecurityKey = "abcdefghijklmnopqrstuvwyxz01234567980";
                var signingKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(plainTextSecurityKey));
                var tokenValidationParameters = new TokenValidationParameters()
                {
                    ValidAudiences = new string[] { "http://localhost:61101" },
                    ValidIssuers = new string[] { "http://localhost:61101" },
                    IssuerSigningKey = signingKey
                };

                SecurityToken validatedToken;
                var claims = tokenHandler.ValidateToken(signedAndEncodedToken,
                    tokenValidationParameters, out validatedToken);
                return new Envelope() { Result = "valid" };

            }
            catch (Exception ex)
            {

                return new Envelope() { Result = "notValid" };
            }
        }


    }
}
