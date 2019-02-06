using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using BuscaPreciosV2.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BuscaPreciosV2.Controllers
{
    [Route("api/[controller]")]
    public class LogController : Controller
    {
        // PUT api/<controller>/5
        /// <summary>
        /// Inserta Log
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        [HttpPost("stats/insert")]
        public async Task<Header> InsertStats(Stats stats)
        {
            try
            {
                stats.Id = Guid.NewGuid().ToString();
                PushResponse response = await client.PushAsync("stats/falabella", stats);
                var name = response.Result.name; //The result will contain the child name of the new data that was added
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"Stats insertado correctamente: {name}"
                };
            }
            catch (Exception ex)
            {
                return new Header()
                {
                    FechaProceso = DateTime.Now,
                    Observación = ex.Message,
                    Correcto = false
                };
            }
        }

        [HttpPut("stats/update")]
        public async Task<Header> UpdateStats(Stats stats)
        {
            try
            {
                var response = await client.UpdateAsync("stats/falabella", stats);
                stats = response.ResultAs<Stats>(); //The response will contain the data written
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"Stats actualizado correctamente"
                };
            }
            catch (Exception ex)
            {
                return new Header()
                {
                    FechaProceso = DateTime.Now,
                    Observación = ex.Message,
                    Correcto = false
                };
            }
        }

        [HttpPost("log/insert")]
        public async Task<Header> InsertLog(LogErrores log)
        {
            try
            {
                PushResponse response = await client.PushAsync("log/falabella", log);
                var name = response.Result.name; //The result will contain the child name of the new data that was added
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"Log insertado correctamente: {name}"
                };
            }
            catch (Exception ex)
            {
                return new Header()
                {
                    FechaProceso = DateTime.Now,
                    Observación = ex.Message,
                    Correcto = false
                };
            }
        }

        [HttpPut("log/update")]
        public async Task<Header> UpdatetLog(LogErrores log)
        {
            try
            {
                var response = await client.UpdateAsync("log/falabella", log);
                log = response.ResultAs<LogErrores>(); //The response will contain the data written
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"Log actualizado correctamente"
                };
            }
            catch (Exception ex)
            {
                return new Header()
                {
                    FechaProceso = DateTime.Now,
                    Observación = ex.Message,
                    Correcto = false
                };
            }
        }


        public IConfiguration Configuration { get; }
        private IFirebaseClient client;

        public LogController(IConfiguration configuration)
        {
            string connString = configuration.GetSection("ConnectionApp").GetSection("FirebaseSecretCode").Value;
            string basePath = configuration.GetSection("ConnectionApp").GetSection("BasePath").Value;

            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = connString,
                BasePath = basePath
            };
            client = new FirebaseClient(config);
        }
    }
}
