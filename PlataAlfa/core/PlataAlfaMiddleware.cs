using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace PlataAlfa.core
{
    public class PlataAlfaMiddleware
    {
        private readonly RequestDelegate _next;

        public PlataAlfaMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Envelope<string> TokenValid(string signedAndEncodedToken)
        {
            try
            {
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
                var user = claims.Claims.Where(item => item.Type.Contains("nameidentifier")).FirstOrDefault().Value;

                return new Envelope<string> { Result = "ok", Data = user };

            }
            catch (Exception ex)
            {
                return new Envelope<string> { Result = "fail", Exception = ex };
            }
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "POST" && context.Request.ContentType == "application/json")
            {
                try
                {
                    context.Request.EnableRewind();
                    string jsonData = new StreamReader(context.Request.Body).ReadToEnd();
                    dynamic pk = JsonConvert.DeserializeObject(jsonData);


                    string version = pk.Version;
                    string token = pk.Token;
                    string area = pk.Area == null ? string.Empty : $"{pk.Area}.";
                    bool pass = area == string.Empty;
                    if (area != string.Empty)
                    {
                        var tokenValidatinResult = TokenValid(token);
                        pass = tokenValidatinResult.Result == "ok";
                        pk.Data.AuthUser = tokenValidatinResult.Data;
                    }

                    if (pass)
                    {
                        string entity = pk.Entity;
                        string action = pk.Action;
                        var data = pk.Data == null ? null : pk.Data;
                        string pathApi = $"PlataAlfa.api.V{version.ToString().Replace('.', '_')}.";
                        pathApi += $"{area}{entity}";
                        string pathData = $"PlataAlfa.data.V{version.ToString().Replace('.', '_')}.";
                        pathData += $"{area}{entity}";

                        var entityObj = Program.Entities.Where(x => x.FullName == pathApi || x.FullName == pathData);
                        if (entityObj.Count() != 0)
                        {
                            MethodInfo actionMethod = entityObj.FirstOrDefault().GetMethod(action);

                            if (actionMethod != null)
                            {
                                Envelope result = null;
                                ParameterInfo[] parameters = actionMethod.GetParameters();
                                object classInstance = Activator.CreateInstance(entityObj.FirstOrDefault(), null);

                                if (parameters.Length == 0)
                                {
                                    result = (Envelope)actionMethod.Invoke(classInstance, null);
                                }
                                else
                                {
                                    object[] parametersArray = new object[parameters.Length];
                                    int index = 0;
                                    foreach (var param in parameters)
                                    {
                                        if (param.Name == "data")
                                        {
                                            parametersArray[index] = data;
                                        }
                                        else
                                        {
                                            var dependencyType = Program.Entities.Where(x => x.FullName == param.ParameterType.FullName);
                                            object dependencyInstance = Activator.CreateInstance(dependencyType.FirstOrDefault(), null);
                                            parametersArray[index] = dependencyInstance;
                                        }
                                        index++;
                                    }

                                    result = (Envelope)actionMethod.Invoke(classInstance, parametersArray);
                                }

                                classInstance = null;
                                //System.GC.Collect();
                                string json = JsonConvert.SerializeObject(result);

                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(json);

                            }
                            else
                            {
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync("Resource action not found!");
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Resource entity not found!");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("User Forbidden");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.Message);
                }

            }
            else
            {
                await this._next(context);
            }
        }
    }
}
