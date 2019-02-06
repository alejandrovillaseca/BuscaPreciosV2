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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BuscaPreciosV2.Controllers
{
    [Route("api/[controller]")]
    public class FalabellaController : Controller
    {
        [HttpPost("urlprocess")]
        public async Task<Response> ProcesaURLAsync(Url _objUrl)
        {
            try
            {
                var _listProductos = new List<ResultList>();
                Response _responseData = await ProcesaProductoPorURLAsync(_objUrl.URL);
                if (!_responseData.Header.Correcto)
                    throw new Exception(_responseData.Header.Observación);

                //Se inserta la primera lista de productos en firebird
                //TODO: Se inserta solo si existen
                await InsertProduct(_responseData.Productos);
                _listProductos = _listProductos.Union(_responseData.Productos).ToList();
                //Ahora tengo que hacer lo mismo para la cantidad de páginas que existan
                for (int i = 2; i <= _objUrl.CantPaginas; i++)
                {
                    var _url = _objUrl.URL + $"&page={i}";
                    _responseData = await ProcesaProductoPorURLAsync(_url);
                    if (!_responseData.Header.Correcto)
                    {
                        await new LogController(Configuration).InsertLog(new LogErrores()
                        {
                            URL = _url,
                            Observaciones = $"Error al obtener información de la URL en ProcesaProductoPorURLAsync. URL: {_url} Page: {i}" + _responseData.Header.Observación
                        });
                        continue;
                    }

                    //Se inserta la otra lista de productos.
                    //TODO insertar solo si no existe...
                    await InsertProduct(_responseData.Productos);
                    _listProductos = _listProductos.Union(_responseData.Productos).ToList();
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
        /// Método que devuelve los objetos deserializados de la página por HttpWebRequest
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpGet("procesapagina")]
        public async Task<Response> ProcesaProductoPorURLAsync(string url)
        {
            try
            {
                var _htmlDocument = await ObtenerHtmlDocumentAsync(url);
                var ini = "fbra_browseProductListConfig";
                var fin = "var fbra_browseProductList =";
                var json = _htmlDocument.Text.Substring(_htmlDocument.Text.IndexOf(ini) + ini.Length + 3, _htmlDocument.Text.IndexOf(fin) - _htmlDocument.Text.IndexOf(ini) - ini.Length - 3);
                json = json.Replace(";", "");

                ProductoResponse _responseData;
                try
                {
                    _responseData = JsonConvert.DeserializeObject<ProductoResponse>(json);
                }
                catch (Exception ex)
                {
                    return new Response()
                    {
                        Header = new Header()
                        {
                            Correcto = false,
                            FechaProceso = DateTime.Now,
                            Observación = $"Lo más probable es que cambió el contrato. {ex.Message}"
                        }
                    };
                }

                return new Response()
                {
                    Header = new Header()
                    {
                        Correcto = true,
                        FechaProceso = DateTime.Now,
                        Observación = "Procesado correctamente",
                    },
                    Productos = _responseData.State.SearchItemList.ResultList,
                    FullObject = _responseData
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

        /// <summary>
        /// Obtiene objeto HtmlDocument de la url pasada, quizás deba ser parte de un handler global....
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<HtmlAgilityPack.HtmlDocument> ObtenerHtmlDocumentAsync(string url)
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
                return htmlDocument;
            }
            catch (Exception ex)
            {
                await new LogController(Configuration).InsertLog(new LogErrores()
                {
                    URL = url,
                    Observaciones = $"Error al obtener información de la URL. Método ObtenerHtmlDocumentAsync. URL: {url} " + ex.Message
                });
                throw new Exception(ex.Message);
            }
        }

        [HttpPost("falabella/product/insert")]
        public async Task<Header> InsertProduct(List<ResultList> producto)
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
        public async Task<Header> UpdateProduct(ResultList producto)
        {
            try
            {
                var response = await client.UpdateAsync("falabella/productos", producto);
                producto = response.ResultAs<ResultList>(); //The response will contain the data written
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

        public async Task<bool> DeleteRepeatProduct(string SKU)
        {
            try
            {
                //FirebaseResponse response;
                var products = await GetProducts(SKU);
                ////foreach (var item in products.Productos)
                ////{
                ////    if (item..Count() < 2)
                ////        return false;

                ////    //foreach (var _item in products.Productos.State.SearchItemList.ResultList)
                ////    //{
                ////    //    response = await client.DeleteAsync("falabella/productos" + item.SkuId);
                ////    //}
                ////}


                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Header> InsertUrl(Url _url)
        {
            try
            {
                PushResponse response = await client.PushAsync("falabella/urls", _url);
                var name = response.Result.name; //The result will contain the child name of the new data that was added
                return new Header()
                {
                    Correcto = true,
                    FechaProceso = DateTime.Now,
                    Observación = $"URL insertada correctamente: {name}"
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

        [HttpGet("products/get")]
        public async Task<Response> GetProducts(string SKU)
        {
            try
            {
                FirebaseResponse response;
                if (SKU == null)
                    response = await client.GetAsync("falabella/productos");
                else
                    response = await client.GetAsync("falabella/productos");

                var products = response.ResultAs<List<ResultList>>(); //The response will contain the data written
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

        /// <summary>
        /// Método que procesa todas las URL encontradas en la página de Falabella CL
        /// </summary>
        /// <returns></returns>
        [HttpGet("geturls")]
        public async Task<UrlResponse> GetUrlsAsync()
        {
            var _lstUrl = new List<Url>();
            try
            {
                var _htmlDocument = await ObtenerHtmlDocumentAsync("https://www.falabella.com/falabella-cl");
                var _menu = _htmlDocument.DocumentNode.SelectNodes(string.Format("//li[@class='fb-masthead__primary-links__item']"));
                foreach (var item in _menu)
                {
                    //Por cada menú, recorro las categorías
                    var cat = new HtmlAgilityPack.HtmlDocument();
                    cat.LoadHtml(item.InnerHtml);
                    var _cat = cat.DocumentNode.SelectNodes(string.Format("//li[@class='fb-masthead__child-links__item']"));
                    foreach (var item2 in _cat)
                    {
                        //Ahora que estoy en la categoría, obtengo el link de "Todos"
                        var _link = new HtmlAgilityPack.HtmlDocument();
                        _link.LoadHtml(item2.InnerHtml);
                        var link = _link.DocumentNode.SelectNodes(string.Format("//li[@class='fb-masthead__grandchild-links__item']"));
                        var url = link.LastOrDefault().SelectNodes("a")[0].Attributes["href"].Value;
                        url = BASE_URL + url + "?isPLP=1"; //No sé por que no viene en el htmlDocument...
                                                           //La inserto en la BD
                                                           //TODO --> Debo validar si la URL ya existe. En caso contrario la agrego:
                                                           //Necesito saber la cantidad de páginas de la url...
                        var _doc = await ProcesaProductoPorURLAsync(url);
                        if (!_doc.Header.Correcto)
                        {
                            await new LogController(Configuration).InsertLog(new LogErrores()
                            {
                                URL = url,
                                Observaciones = "Error al ProcesaProductoPorURLAsync: " + _doc.Header.Observación
                            });
                            continue;
                        }

                        var _url = new Models.Url()
                        {
                            URL = url,
                            CantPaginas = _doc.FullObject.State.SearchItemList.PagesTotal
                        };
                        //TODO insertar solo si existe
                        await InsertUrl(_url);
                        _lstUrl.Add(_url);
                    }
                }
                return new UrlResponse()
                {
                    Header = new Header()
                    {
                        Correcto = true,
                        FechaProceso = DateTime.Now,
                        Observación = "URL's procesadas correctamente",
                    },
                    Urls = _lstUrl
                };
            }
            catch (Exception ex)
            {
                return new UrlResponse()
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

        /// <summary>
        /// Método que procesa una URL
        /// </summary>
        /// <returns></returns>
        [HttpPost("geturl")]
        public async Task<UrlResponse> GetUrlAsync(string url)
        {
            var _lstUrl = new List<Url>();
            try
            {
                var _doc = await ProcesaProductoPorURLAsync(url);
                if (!_doc.Header.Correcto)
                {
                    await new LogController(Configuration).InsertLog(new LogErrores()
                    {
                        URL = url,
                        Observaciones = "Error al ProcesaProductoPorURLAsync: " + _doc.Header.Observación
                    });
                    throw new Exception(_doc.Header.Observación);
                }

                var _url = new Models.Url()
                {
                    URL = url,
                    CantPaginas = _doc.FullObject.State.SearchItemList.PagesTotal
                };
                //TODO insertar solo si existe...
                await InsertUrl(_url);
                _lstUrl.Add(_url);

                return new UrlResponse()
                {
                    Header = new Header()
                    {
                        Correcto = true,
                        FechaProceso = DateTime.Now,
                        Observación = "URL procesadas correctamente",
                    },
                    Urls = _lstUrl
                };
            }
            catch (Exception ex)
            {
                return new UrlResponse()
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
        const string BASE_URL = "https://www.falabella.com";
        public FalabellaController(IConfiguration configuration)
        {

            string connString = configuration.GetSection("ConnectionApp").GetSection("FirebaseSecretCode").Value;
            string basePath = configuration.GetSection("ConnectionApp").GetSection("BasePath").Value;
            Configuration = configuration;
            IFirebaseConfig config = new FirebaseConfig
            {
                AuthSecret = connString,
                BasePath = basePath
            };
            client = new FirebaseClient(config);
        }

    }
}
