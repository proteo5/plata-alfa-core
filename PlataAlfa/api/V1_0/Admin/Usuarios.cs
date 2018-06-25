using Newtonsoft.Json;
using PlataAlfa.core;
using PlataAlfa.data.V1_0.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlataAlfa.api.V1_0.Admin
{
    public class Usuarios : Entity
    {
        public Envelope CreateAdmin(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                var adminUserExist = usuariosDS.GetByUsuario(new { Usuario = "admin" });
                if (adminUserExist.Result == "empty")
                {
                    dynamic data2 = JsonConvert.DeserializeObject("{}");
                    data2.Usuario = "admin";
                    data2.Password = "123";
                    data2.Nombre = "Administrator";
                    data2.Apellidos = "del Sistema";
                    data2.Email = "";
                    data2.IsActive = true;
                    var result = this.Create(data2, usuariosDS);

                    return result;
                }
                else
                {
                    return new Envelope() { Result = "notSuccess", Message = "Admin user already exist." };
                }
            }
            catch (Exception ex)
            {
                return new Envelope() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<dynamic> Create(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                var result = usuariosDS.GetByUsuario(data);
                if (result.Result == "ok")
                {
                    return new Envelope<dynamic> { Result = "notSuccess", Message = "El Usuario ya Existe" };
                }
                else
                {
                    dynamic data2 = JsonConvert.DeserializeObject("{}");
                    data2.Nombre = data.Nombre;
                    data2.Apellidos = data.Apellidos;
                    data2.Email = data.Email;
                    data2.Usuario = data.Usuario.ToString().ToLower();
                    data2.PasswordSalt = Guid.NewGuid().ToString();
                    data2.Password = HashHL.SHA256Of($"{data2.Usuario}123{data2.PasswordSalt}");
                    data2.IsActive = true;
                    var response = usuariosDS.Insert(data2);

                    return response;
                }
            }
            catch (Exception ex)
            {
                return new Envelope<dynamic>() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<dynamic> Update(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                //This is done to prevent the update of fileds other than needed
                dynamic data2 = JsonConvert.DeserializeObject("{}");
                data2.iUsuario = data.iUsuario.ToString();
                data2.Nombre = data.Nombre.ToString();
                data2.Apellidos = data.Apellidos.ToString();
                data2.Email = data.Email.ToString();
                data2.IsActive = (bool)data.IsActive;

                var result = usuariosDS.Update(data2);
                return result;
            }
            catch (Exception ex)
            {
                return new Envelope<dynamic>() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<List<dynamic>> GetAll(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                var result = usuariosDS.GetDataSet(fields: "[iUsuario],[Usuario],[Nombre],[Apellidos],[Email],[IsActive]");
                return result;
            }
            catch (Exception ex)
            {
                return new Envelope<List<dynamic>>() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<List<dynamic>> GetByID(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                string options = $" WHERE iUsuario = '{data.id}' ";
                var result = usuariosDS.GetDataSet(fields: "[iUsuario],[Usuario],[Nombre],[Apellidos],[Email],[IsActive]", options: options);
                return result;
            }
            catch (Exception ex)
            {
                return new Envelope<List<dynamic>>() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<List<dynamic>> GetProfile(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                string options = $" WHERE Usuario = '{data.AuthUser}' ";
                var result = usuariosDS.GetDataSet(fields: "[iUsuario],[Usuario],[Nombre],[Apellidos],[Email]", options: options);
                return result;
            }
            catch (Exception ex)
            {
                return new Envelope<List<dynamic>>() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<dynamic> UpdateProfile(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                var userResult = this.GetProfile(data, usuariosDS);
                var user = userResult.Data[0];

                //This is done to prevent the update of fileds other than needed
                dynamic data2 = JsonConvert.DeserializeObject("{}");
                data2.id = user.iUsuario;
                data2.Nombre = data.Nombre;
                data2.Apellidos = data.Apellidos;
                data2.Email = data.Email;

                var updateResponse = usuariosDS.Update(data2);
                return updateResponse;
            }
            catch (Exception ex)
            {
                return new Envelope<dynamic>() { Result = "error", Message = ex.Message };
            }
        }

        public Envelope<dynamic> CambiarPass(dynamic data, UsuariosDS usuariosDS)
        {
            try
            {
                string usuario = data.AuthUser;
                string password = data.previoPassword;
                string options = $" WHERE Usuario = '{usuario}' ";
                var requestUser = usuariosDS.GetDataSet(fields: "[iUsuario],[Usuario],[Password],[PasswordSalt]", options: options);

                if (requestUser.Result != "ok")
                {
                    return new Envelope<dynamic>() { Result = "notSuccess", Message = "User not found" };
                }

                var dataSet = requestUser.Data.FirstOrDefault();
                if (HashHL.SHA256Of($"{usuario}{password}{dataSet.PasswordSalt}") != dataSet.Password &&
                    dataSet.Password != password)
                {
                    return new Envelope<dynamic>() { Result = "notSuccess", Message = "Wrong passord" };
                }
                else
                {
                    dynamic data2 = JsonConvert.DeserializeObject("{}");
                    data2.id = dataSet.iUsuario;
                    data2.PasswordSalt = Guid.NewGuid().ToString(); ;
                    data2.Password = HashHL.SHA256Of($"{usuario}{data.nuevoPassword}{data2.PasswordSalt}"); ;

                    var updateResponse = usuariosDS.Update(data2);
                    return updateResponse;
                }
            }
            catch (Exception ex)
            {
                return new Envelope<dynamic>() { Result = "error", Message = ex.Message };
            }
        }

    }
}
