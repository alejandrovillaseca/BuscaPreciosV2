using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BuscaPreciosV2.Controllers
{
    [Route("api/[controller]")]
    public class FalabellaController : Controller
    {   
        [HttpGet]
        public Models.Producto ProcesaURL(string url)
        {
            HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
            string source = string.Empty;
            try
            {
                WebRequest req = HttpWebRequest.Create(url);
                req.Method = "GET";

                using (StreamReader reader = new StreamReader(req.GetResponse().GetResponseStream()))
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
                    idProceso = 1, //TODO
                    idSistema = 1,
                    URL = url,
                    Observaciones = "ProcesaSoloPorURL: Error al obtener información de la URL. " + ex.Message
                };
                //await new LogController().InsertLog(_log);

                //CantidadProductos = 0;
                //_htmlDocument = htmlDocument; //devuelve la variable hacia afuera del método.
                return new Models.Producto()
                {
                    Correcto = false,
                    FechaProceso = DateTime.Now,
                    Observaciones = _log.Observaciones
                };
            }

            //ahora que tenemos todo el codigo, seleccionamos donde deberían estar los productos
            var nodes = htmlDocument.DocumentNode.SelectNodes(string.Format("//div[@class='cajaLP4x']"));
            if (nodes == null)
            {
                //_htmlDocument = null;
                //CantidadProductos = 0;
                return new Models.Producto()
                {
                    Correcto = false,
                    FechaProceso = DateTime.Now,
                    Observaciones = "No se encontraron productos"
                };
            }

            var _listProductos = new List<Models.Producto>();

            foreach (var item in nodes)
            {
                //Ahora estoy en el producto
                Models.Producto _obj = new Models.Producto();
                //Inserto el producto en null
                _obj.Correcto = false;
                _obj.NombreProducto = "Producto Sin Nombre";
                _obj.Marca = "Producto Sin Marca";
                _obj.Link = url;
                _obj.idURL = 1; //TODO
                _obj.Observaciones = "Producto para ser procesado";
                _obj.FechaProceso = DateTime.Now;
                _obj.idProceso = 1; //TODO
                int idProducto;
                Component.ProductoComponent.InsertarProducto(_obj, out idProducto);
                _obj.id = idProducto;
                try
                {
                    HtmlAgilityPack.HtmlDocument htmlProduct = new HtmlAgilityPack.HtmlDocument();
                    htmlProduct.LoadHtml(item.InnerHtml);

                    //Precios
                    var precios = htmlProduct.DocumentNode.SelectNodes(string.Format("//span[@class='unitPriceD']"));
                    if (precios == null)
                    {
                        //No hay preicos aqui, intentaremos buscar de otra forma
                        precios = htmlProduct.DocumentNode.SelectNodes(string.Format("//div[@class='wishlistPrice1']"));
                    }
                    if (precios.Count > 2)
                    {
                        //Es por que tiene "Oportunidad única en CMR"
                        _obj.Precio = Convert.ToInt32(precios[0].InnerHtml.Replace("$", "").Replace(".", ""));
                        _obj.PrecioInternet = Convert.ToInt32(precios[1].InnerHtml.Replace("$", "").Replace(".", ""));
                        _obj.PrecioNormal = Convert.ToInt32(precios[2].InnerHtml.Replace("$", "").Replace(".", ""));
                        _obj.DescuentoCMR = true;
                    }
                    else if (precios.Count == 1)
                    {
                        _obj.Precio = 0;
                        try
                        {
                            int _preciointernet;
                            if (precios[0].InnerHtml.Contains("div class"))
                            {
                                _preciointernet = Convert.ToInt32(precios[0].InnerHtml.Substring(precios[0].InnerHtml.IndexOf("$"), precios[0].InnerHtml.Length - precios[0].InnerHtml.IndexOf("$") - 19).Replace("$", "").Replace(".", ""));
                            }
                            else
                            {
                                _preciointernet = Convert.ToInt32(precios[0].InnerHtml.Replace("$", "").Replace(".", ""));
                            }
                            _obj.PrecioInternet = _preciointernet;
                        }
                        catch (Exception)
                        {
                            _obj.PrecioInternet = 0;
                            _obj.Observaciones = "No se pudo obtener el precio";
                        }

                        _obj.PrecioNormal = 0;
                        _obj.DescuentoCMR = false;
                    }
                    else
                    {
                        _obj.Precio = 0;
                        _obj.PrecioInternet = Convert.ToInt32(precios[0].InnerHtml.Replace("$", "").Replace(".", ""));
                        _obj.PrecioNormal = Convert.ToInt32(precios[1].InnerHtml.Replace("$", "").Replace(".", ""));
                        _obj.DescuentoCMR = false;
                    }

                    //Nombre
                    var nombreProducto = htmlProduct.DocumentNode.SelectNodes(string.Format("//div[@class='detalle']"));
                    _obj.NombreProducto = nombreProducto[0].InnerText.Replace("\r\n\t\t", String.Empty);

                    //Código producto
                    var codigoProducto = htmlProduct.DocumentNode.OuterHtml.ToString().Substring(htmlProduct.DocumentNode.OuterHtml.ToString().IndexOf(@"<div id=""desc_") + 14, 16);
                    _obj.CodigoProcudto = codigoProducto.Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace("\\", "").Replace(">", "").Replace(@"""", "").TrimEnd();
                    //Si ya existe el mismo producto en el mismo proceso, lo eliminamos
                    bool repetido = Component.ProductoComponent.EliminaProductosRepetidos(_obj.idProceso, _obj.CodigoProcudto);
                    if (repetido == true) continue;


                    //Marca
                    var marcaProducto = htmlProduct.DocumentNode.SelectNodes(string.Format("//div[@class='marca']"));
                    _obj.Marca = marcaProducto[0].InnerText.Replace("\r\n\t\t", String.Empty);

                    //Link
                    _obj.Link = nombreProducto[0].InnerHtml;
                    _obj.Link = "http://www.falabella.com" + _obj.Link.Substring(_obj.Link.IndexOf("href") + 6, _obj.Link.IndexOf(@""">") - _obj.Link.IndexOf("href") - 6);

                    //Lo agregamos a la base de datos
                    ModificaProducto(_obj);
                    _listProductos.Add(_obj);
                }
                catch (Exception ex)
                {
                    //Marcamos como error
                    _obj.Observaciones = "ProcesaSoloPorURL: " + ex.Message;
                    _obj.Correcto = false;
                    Component.ProductoComponent.ModificarProducto(_obj);
                    //throw new Exception(_obj.Observaciones);
                    //CantidadProductos = _listProductos.Count; //TODO
                    //_htmlDocument = htmlDocument; //TODO
                    return new Models.Producto()
                    {
                        Correcto = false,
                        FechaProceso = DateTime.Now,
                        Observaciones = _obj.Observaciones
                    };
                }

            }

            //CantidadProductos = _listProductos.Count; //TODO
            //_htmlDocument = htmlDocument; //TODO
            
            return new Models.Producto()
            {
                Correcto = true,
                FechaProceso = DateTime.Now,
                Observaciones = "Lista De Productos Generada Correctamente",
                //Productos = _listProductos //TODO
            };
        }

        public IConfiguration Configuration { get; }
        private IFirebaseClient client;
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
