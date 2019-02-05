using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BuscaPreciosV2.Models;
using BuscaPreciosV2.Models.Falabella;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BuscaPreciosV2.Controllers
{
    [Route("api/[controller]")]
    public class FalabellaController : Controller
    {
        [HttpPost("urlprocess")]
        public async Task<Response> ProcesaURLAsync(string url)
        {
            try
            {
                var _listProductos = new List<ProductoResponse>();
                ResponsePorPagina _responseData = await ProcesaProductoPorURLAsync(url);
                if (!_responseData.Header.Correcto)
                    throw new Exception(_responseData.Header.Observación);

                //Se inserta la primera lista de productos.
                await InsertProduct(_responseData.ProductosPorPágina);
                _listProductos.Add(_responseData.ProductosPorPágina);
                //Ahora tengo que hacer lo mismo para la cantidad de páginas que existan
                var cantPaginas = _responseData.ProductosPorPágina.State.SearchItemList.PagesTotal;
                for (int i = 2; i <= cantPaginas; i++)
                {
                    var _url = url + $"?page={i}";
                    _responseData = await ProcesaProductoPorURLAsync(_url);
                    if (!_responseData.Header.Correcto)
                    {
                        var _log = new LogErrores()
                        {
                            URL = _url,
                            Observaciones = "Error al obtener información de la URL. " + _responseData.Header.Observación
                        };
                        await _logController.InsertLog(_log);
                    }

                    //Se inserta la otra lista de productos.
                    await InsertProduct(_responseData.ProductosPorPágina);
                    _listProductos.Add(_responseData.ProductosPorPágina);
                }

                return new Response()
                {
                    Header = new Header()
                    {
                        Correcto = true,
                        FechaProceso = DateTime.Now,
                        Observación = "Productos cargados correctamente",
                    },
                    Productos = _listProductos
                };
            }
            catch (Exception ex)
            {
                return new Response()
                {
                    Header = new Header()
                    {
                        Correcto = true,
                        FechaProceso = DateTime.Now,
                        Observación = ex.Message
                    }
                };
            }
        }
        /// <summary>
        /// Método que devuelve los objetos deserializados de la consulta por HttpWebRequest
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<ResponsePorPagina> ProcesaProductoPorURLAsync(string url)
        {
            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            string source = string.Empty;
            try
            {
                WebRequest req = HttpWebRequest.Create(url);
                req.Method = "GET";


                using (var reader = new StreamReader(req.GetResponse().GetResponseStream()))
                {
                    source = reader.ReadToEnd();
                }

                htmlDocument.LoadHtml(source);
            }
            catch (Exception ex)
            {
                //Marcamos el log como error:
                Models.LogErrores _log = new Models.LogErrores()
                {
                    URL = url,
                    Observaciones = "Error al obtener información de la URL. " + ex.Message
                };
                await _logController.InsertLog(_log);

                //CantidadProductos = 0;
                //_htmlDocument = htmlDocument; //devuelve la variable hacia afuera del método.
                return new ResponsePorPagina()
                {
                    Header = new Header()
                    {
                        Correcto = false,
                        FechaProceso = DateTime.Now,
                        Observación = _log.Observaciones
                    }
                };
            }
            var ini = "fbra_browseProductListConfig";
            var fin = "var fbra_browseProductList =";
            var json = htmlDocument.Text.Substring(htmlDocument.Text.IndexOf(ini) + ini.Length + 3, htmlDocument.Text.IndexOf(fin) - htmlDocument.Text.IndexOf(ini) - ini.Length - 3);
            json = json.Replace(";", "");

            ProductoResponse _responseData;
            try
            {
                _responseData = JsonConvert.DeserializeObject<ProductoResponse>(json);
            }
            catch (Exception ex)
            {
                return new ResponsePorPagina()
                {
                    Header = new Header()
                    {
                        Correcto = false,
                        FechaProceso = DateTime.Now,
                        Observación = $"Lo más probable es que cambió el contrato. {ex.Message}"
                    }
                };
            }
            return new ResponsePorPagina()
            {
                Header = new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = "Procesado correctamente",
                },               
                ProductosPorPágina = _responseData
            };
        }

        [HttpPost("falabella/product/insert")]
        public async Task<Header> InsertProduct(ProductoResponse producto)
        {
            try
            {
                PushResponse response = await client.PushAsync("falabella/productos", producto);
                var name = response.Result.name; //The result will contain the child name of the new data that was added
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"Producto insertado correctamente: {name}"
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
        [HttpPut("falabella/product/update")]
        public async Task<Header> UpdateProduct(Producto producto)
        {
            try
            {
                var response = await client.UpdateAsync("falabella/productos", producto);
                producto = response.ResultAs<Producto>(); //The response will contain the data written
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"Producto actualizado correctamente"
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

        public async Task<bool> DeleteRepeatProduct(int idProceso, string CodigoProducto)
        {
            try
            {
                FirebaseResponse response;
                var products = await ListProducts(CodigoProducto, idProceso);
                foreach (var item in products.Productos)
                {
                    if (item.State.SearchItemList.ResultList.Count() < 2)
                        return false;

                    //foreach (var _item in products.Productos.State.SearchItemList.ResultList)
                    //{
                    //    response = await client.DeleteAsync("falabella/productos" + item.SkuId);
                    //}
                }


                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        [HttpGet("falabella/product/list")]
        public async Task<Response> ListProducts(string CodigoProducto = null, int? idProceso = null)
        {
            try
            {
                FirebaseResponse response;
                if (idProceso == null && CodigoProducto == null)
                    response = await client.GetAsync("falabella/productos");
                else
                    response = await client.GetAsync("falabella/productos/" + idProceso);

                var products = response.ResultAs<List<ProductoResponse>>(); //The response will contain the data written
                return new Response()
                {
                    Header = new Header()
                    {
                        Correcto = true,
                        FechaProceso = DateTime.Now,
                        Observación = "Lista generada correctamente",
                    },                    
                    Productos = products
                };
            }
            catch (Exception ex)
            {
                return new Response()
                {
                    Header = new Header()
                    {
                        Correcto = false,
                        FechaProceso = DateTime.Now,
                        Observación = ex.Message
                    }
                };
            }
        }



        public IConfiguration Configuration { get; }
        private IFirebaseClient client;
        public LogController _logController;
        public FalabellaController(IConfiguration configuration)
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
