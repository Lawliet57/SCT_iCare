﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using conekta;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SCT_iCare;
using System.IO;
using System.Text;
using System.Globalization;
using MessagingToolkit.QRCode.Codec;
using System.Drawing;
using System.IO.Compression;
using PagedList;


namespace SCT_iCare.Controllers.Recepcion
{
    public class RecepcionController : Controller
    {
        private GMIEntities db = new GMIEntities();

        public static void GetApiKey()
        {
            conekta.Api.apiKey = ConfigurationManager.AppSettings["conekta"];
            conekta.Api.version = "2.0.0";
            conekta.Api.locale = "es";
        }

        // GET: Pacientes
        public ActionResult Index(DateTime? inicio, DateTime? final, string sucursal)
        {
            DateTime thisDate = new DateTime();
            DateTime tomorrowDate = new DateTime();

            DateTime start1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            DateTime finish1 = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day);

            int nulos = 0;

            if (inicio != null || final != null)
            {
                nulos = 1;
            }

            if (inicio != null)
            {
                DateTime start = Convert.ToDateTime(inicio);
                int year = start.Year;
                int month = start.Month;
                int day = start.Day;

                inicio = new DateTime(year, month, day);
                thisDate = new DateTime(year, month, day);
            }
            if (final != null)
            {
                DateTime finish = Convert.ToDateTime(final).AddDays(1);
                int year = finish.Year;
                int month = finish.Month;
                int day = finish.Day;

                final = new DateTime(year, month, day);
                tomorrowDate = new DateTime(year, month, day);
            }

            var urge = (from i in db.UrgentesCount select i).FirstOrDefault();
            string mes = DateTime.Now.ToString("MMMM");

            if (urge.Mes != mes)
            {
                urge.Mes = mes;
                urge.Contador = 500;

                if (ModelState.IsValid)
                {
                    db.Entry(urge).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            inicio = (inicio ?? start1);
            final = (final ?? finish1);

            ViewBag.Inicio = inicio;
            ViewBag.Final = final;
            ViewBag.Estado = nulos;
            ViewBag.Sucursal = sucursal;

            ViewBag.Parameter = "";

            return View(db.Paciente.ToList());
        }

        public ActionResult Dashboard()
        {
            return View(db.Paciente.ToList());
        }

        public ActionResult NextDay()
        {
            return View(db.Paciente.ToList());
        }

        public ActionResult Foto()
        {
            return View(db.Paciente.ToList());
        }


        public ActionResult CargarFoto(HttpPostedFileBase file, string id)
        {
            int ide = Convert.ToInt32(id);

            var revisionFoto = (from i in db.Biometricos where i.idPaciente == ide select i).FirstOrDefault();

            byte[] bytes2 = null;

            if (file != null && file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(file.InputStream))
                {
                    bytes = br.ReadBytes(file.ContentLength);
                }

                bytes2 = bytes;
            }

            if (revisionFoto == null)
            {
                Biometricos bio = new Biometricos();

                bio.Foto = bytes2;
                bio.idPaciente = ide;

                if (ModelState.IsValid)
                {
                    db.Biometricos.Add(bio);
                    db.SaveChanges();
                }
            }
            else
            {
                var bio = db.Biometricos.Find(revisionFoto.idBiometricos);

                bio.Foto = bytes2;

                if (ModelState.IsValid)
                {
                    db.Entry(bio).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Foto");
        }

        public ActionResult CambiarGestor(int? id, int? referido, string usuario, string gestorAnterior, string tipoPago, string referencia, HttpPostedFileBase ticket, int? ide)
        {
            Cita cita = db.Cita.Find(id);

            var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
            string precioEstablecido;
            var pagoAnterior = cita.TipoPago;
            var refrenciaAnterior = cita.Referencia;
            string IVA;
            string totalVentasIVA;
            string haySeguro = cita.ventaSeguro != "SI" ? cita.CostoSeguro : null;
            string totalIVA;
            string epiIVA = cita.IVA == null ? null : cita.IVA;
            string IVASeguro = cita.IvaSeguro != null ? cita.IvaSeguro : null;
            string totalVentacIVA = null;
            string totalSeguro = cita.TotalSeguro == null ? null : cita.TotalSeguro;
            string precioEpiCompleto;

            if (tipoPago == "REFERENCIA OXXO" || tipoPago == "Pago con Tarjeta" || tipoPago == "Referencia OXXO" || tipoPago == "Transferencia vía BanBajío" 
                || tipoPago == "Mercado Pago" || tipoPago == "Credito Empresas" || tipoPago == "Referencia BanBajío" || tipoPago == "Referencía BanBajío" || tipoPago == "Banorte")
            {
                if (cita.TipoTramite == "AEREO")
                {
                    precioEstablecido = referidoTipo.PrecioAereo;
                }
                else if (cita.TipoTramite == "AEREO_PISTA")
                {
                    precioEstablecido = referidoTipo.PrecioAereoPistaconIVA;
                }
                else
                {
                    precioEstablecido = referidoTipo.PrecioNormalconIVA;
                }

                precioEpiCompleto = precioEstablecido;
                IVA = (Convert.ToDouble(precioEstablecido) - (Convert.ToDouble(precioEstablecido) / 1.16)).ToString("####.##");
                epiIVA = IVA;
                precioEstablecido = (Convert.ToDouble(precioEstablecido) / 1.16).ToString("####.##");
            }
            else
            {
                if (cita.TipoTramite == "AEREO")
                {
                    precioEstablecido = referidoTipo.PrecioAereosinIVA;
                }
                else if (cita.TipoTramite == "AEREO_PISTA")
                {
                    precioEstablecido = referidoTipo.PrecioAereoPista;
                }
                else
                {
                    precioEstablecido = referidoTipo.PrecioNormal;
                }

                precioEpiCompleto = precioEstablecido;
                IVA = null;
                epiIVA = IVA;
            }

            if (haySeguro == null)
            {
                totalVentasIVA = precioEstablecido;
            }
            else
            {
                totalVentasIVA = (Convert.ToDouble(precioEstablecido) + Convert.ToDouble(haySeguro)).ToString("####.##");
            }

            if (epiIVA == null && IVASeguro == null)
            {
                totalIVA = null;
                totalVentacIVA =  (Convert.ToDouble(totalVentasIVA) + Convert.ToDouble(IVA)).ToString("####.##");
            }
            else
            {
                totalIVA = (Convert.ToDouble(IVA) + Convert.ToDouble(IVASeguro)).ToString("####.##");
                totalVentacIVA = (Convert.ToDouble(precioEstablecido) + Convert.ToDouble(IVA) + Convert.ToDouble(totalSeguro)).ToString("####.##");
            }

            if (referidoTipo.Tipo != "OTRO")
            {
                cita.CostoSeguro = null;
                cita.ventaSeguro = null;
                cita.TotalSeguro = null;
                cita.IvaSeguro = null;
            }

            cita.TotalIVA = totalIVA; 
            cita.TotalVentaIVA = totalVentacIVA;
            cita.TotalVenta = totalVentasIVA;
            cita.IVA = IVA;
            cita.PrecioEpi = precioEpiCompleto;
            cita.Venta = precioEstablecido;
            cita.CC = referidoTipo.Tipo;
            cita.CanalTipo = referidoTipo.Tipo;            
            cita.ReferidoPor = referidoTipo.Nombre;
            string cambiaReferido = gestorAnterior == referidoTipo.Nombre ? null : " Gestor de " + gestorAnterior + " a " + referidoTipo.Nombre;
            cita.TipoPago = tipoPago == "" || tipoPago == cita.TipoPago ? cita.TipoPago : tipoPago;
            string cambiaPago = pagoAnterior == tipoPago ? null : " Tipo de pago de " + pagoAnterior + " a " + tipoPago;
            cita.Referencia = referencia == "" ? cita.Referencia : referencia;
            string cambioReferencia = (refrenciaAnterior == referencia) || (referencia == "") ? null : " Referencia de pago de " + refrenciaAnterior + " a " + referencia;
            string cadenaFinal = cambiaReferido + cambiaPago + cambioReferencia;

            cita.CancelaComentario = cita.CancelaComentario + " + " + usuario + " Modifico los siguientes datos; " + cadenaFinal + " Ticket de pago el día " + DateTime.Now.ToString("dd-MM-yy");       

            if (ModelState.IsValid)
            {
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();
            }

            //MACRO TABLA
            var tablaCita = (from r in db.Cita where r.idCita == id select r).FirstOrDefault();
            var condensadoPaciente = (from r in db.CondensadoPaciente where r.idCita == id select r).FirstOrDefault();

            if (condensadoPaciente != null)
            {
                condensadoPaciente.CostoSeguro = tablaCita.CostoSeguro;
                condensadoPaciente.ventaSeguro = tablaCita.ventaSeguro;
                condensadoPaciente.TotalSeguro = tablaCita.TotalSeguro;
                condensadoPaciente.IvaSeguro = tablaCita.IvaSeguro;
                condensadoPaciente.TotalIVA = tablaCita.TotalIVA;
                condensadoPaciente.TotalVentaIVA = tablaCita.TotalVentaIVA;
                condensadoPaciente.TotalVenta = tablaCita.TotalVenta;
                condensadoPaciente.IVA = tablaCita.IVA;
                condensadoPaciente.Venta = tablaCita.Venta;
                condensadoPaciente.CC = tablaCita.CC;
                condensadoPaciente.CanalTipo = tablaCita.CanalTipo;
                condensadoPaciente.ReferidoPor = tablaCita.ReferidoPor;
                condensadoPaciente.CancelaComentario = tablaCita.CancelaComentario;
                condensadoPaciente.TipoPago = tablaCita.TipoPago;
                condensadoPaciente.Referencia = tablaCita.Referencia;

                if (ModelState.IsValid)
                {
                    db.Entry(condensadoPaciente).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }


            Tickets ticketP = new Tickets();

            byte[] bytes = null;

            var consultaTicket = db.Tickets.Where(w => w.idPaciente == ide).Select(s => new { s.idPaciente, s.idTicket }).FirstOrDefault();

            if (ticket != null && ticket.ContentLength > 0)
            {
                var fileName = Path.GetFileName(ticket.FileName);

                byte[] bytes2;

                using (BinaryReader br = new BinaryReader(ticket.InputStream))
                {
                    bytes2 = br.ReadBytes(ticket.ContentLength);

                    bytes = bytes2;
                }

                ticketP.Ticket = bytes;
                ticketP.idPaciente = Convert.ToInt32(ide);
                ticketP.FechaCarga = DateTime.Now;

                if(consultaTicket == null)
                {
                    if (ModelState.IsValid)
                    {
                        db.Tickets.Add(ticketP);
                        db.SaveChanges();
                    }
                }
                else 
                {
                    Tickets removeT = db.Tickets.Find(consultaTicket.idTicket);
                    db.Tickets.Remove(removeT);
                    db.SaveChanges();

                    if (ModelState.IsValid)
                    {
                        db.Tickets.Add(ticketP);
                        db.SaveChanges();
                    }
                }
            }

            return Redirect("Index");
        }

        public PartialViewResult modificacionBuscador(string expediente)               //CONTROLADOR Y METODO A EJECUTAR CON AJAX
        {
            var paciente = (from r in db.Cita where r.NoExpediente == expediente select r).FirstOrDefault();
            var pacienteCap = (from r in db.Captura where r.NoExpediente == expediente select r).FirstOrDefault();

            if (paciente == null || pacienteCap == null)
            {
                return PartialView("_NoEncontrados");
            }

            else
            {
                var personas = new List<Persona>()
                {
                    new Persona() {Nombre = pacienteCap.NombrePaciente, Sucursal = paciente.Sucursal, TipoTramite = paciente.TipoTramite, TipoLicencia = paciente.TipoLicencia,
                       canalTipo = paciente.CanalTipo, referidoPor = paciente.ReferidoPor, tipoPago = paciente.TipoPago, referencia = paciente.Referencia,
                       idPaciente = Convert.ToInt32(paciente.idPaciente), idCita = Convert.ToInt32(paciente.idCita),  FechaCita = Convert.ToDateTime(paciente.FechaCita).ToString("dd-MMMM-yyyy")}
                };

                return PartialView("_Modificacion", personas);            //VISTA PARCIAL LA CUAL SERA AGREGADA A LA VISTA
            }
        }

        public class Persona   //METODO PARA BUSCAR A LA PERSONA Y SUS DATOS
        {
            public string Nombre { get; set; }
            public string Sucursal { get; set; }
            public string TipoTramite { get; set; }
            public string TipoLicencia { get; set; }
            public string EstatusDictamen { get; set; }
            public string FechaCita { get; set; }
            public string canalTipo { get; set; }
            public string referidoPor { get; set; }
            public string tipoPago { get; set; }
            public string referencia { get; set; }
            public int idPaciente { get; set; }
            public int idCita { get; set; }
        }

        // POST: Pacientes/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.

        public ActionResult VentanaPago(int precioN, int precioNIVA, int precioAT, int precioATIVA, int precioP, int precioPIVA, int precioIngresado, int sumaGestor, string nombreGestor, string nombrePaciente, string emailPaciente,
            string telefonoPaciente, string fechaSolicitud, string fechaExpiracion, string Sucursal, string cantidadEpis, string cantidadEpisA, string cantidadEpisAP)
        {
            ViewBag.precioN = precioN;
            ViewBag.precioNIVA = precioNIVA;
            ViewBag.precioAT = precioAT;
            ViewBag.precioATIVA = precioATIVA;
            ViewBag.precioP = precioP;
            ViewBag.precioPIVA = precioPIVA;
            ViewBag.precioIngresado = precioIngresado;
            ViewBag.sumaGestor = sumaGestor;
            ViewBag.nombreGestor = nombreGestor;
            ViewBag.nombrePaciente = nombrePaciente;
            ViewBag.emailPaciente = emailPaciente;
            ViewBag.telefonoPaciente = telefonoPaciente;
            ViewBag.fechaSolicitud = fechaSolicitud;
            ViewBag.fechaExpiracion = fechaExpiracion;
            ViewBag.Sucursal = Sucursal;
            ViewBag.cantidadEpis = cantidadEpis;
            ViewBag.cantidadEpisA = cantidadEpisA;
            ViewBag.cantidadEpisAP = cantidadEpisAP;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create1(string nombre, string telefono, string email, string usuario, string sucursal, string cantidad, string cantidadAereo, string cantidadPista,
            string pago, string referencia, int? referido, DateTime? fecha/*, int precioIngresado*/, string cb_Seguro)
        {
            var findGestor = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
            var precioN = Convert.ToInt32(findGestor.PrecioNormal);
            var precioNIVA = Convert.ToInt32(findGestor.PrecioNormalconIVA);
            var precioAT = Convert.ToInt32(findGestor.PrecioAereosinIVA);
            var precioATIVA = Convert.ToInt32(findGestor.PrecioAereo);
            var precioP = Convert.ToInt32(findGestor.PrecioAereoPista);
            var precioPIVA = Convert.ToInt32(findGestor.PrecioAereoPistaconIVA);
            var cantidadInt = cantidad != "" ? Convert.ToInt32(cantidad) : 0;
            var cantidadATInt = cantidadAereo != "" ? Convert.ToInt32(cantidadAereo) : 0;
            var cantidadAPInt = cantidadPista != "" ? Convert.ToInt32(cantidadPista) : 0;
            var precioReal = 0;
            var sumaEPISN = 0;
            var sumaEPISAT = 0;
            var sumaEPISAP = 0;
            string condicionante = "";
            ViewBag.precioN = precioN;
            ViewBag.precioNIVA = precioNIVA;
            ViewBag.precioAT = precioAT;
            ViewBag.precioATIVA = precioATIVA;
            ViewBag.precioP = precioP;
            ViewBag.precioPIVA = precioPIVA;
            var fechaEx = Convert.ToDateTime(fecha).AddYears(2);
            var tipoGestor = findGestor.Tipo;
            var IVAS = "";

            if (pago == "REFERENCIA OXXO" || pago == "Pago con Tarjeta" || pago == "Referencia OXXO" || pago == "Transferencia vía BanBajío" || tipoGestor == "EMPRESA"
                || pago == "Mercado Pago" || pago == "Credito Empresas" || pago == "Referencia BanBajío" || pago == "Referencía BanBajío" || pago == "Banorte")
            {
                //if (cantidadInt != 0)
                //{
                //    sumaEPISN = (cantidadInt * precioNIVA);
                //}
                //if (cantidadATInt != 0)
                //{
                //    sumaEPISAT = (cantidadATInt * precioATIVA);
                //}
                //if (cantidadAPInt != 0)
                //{
                //    sumaEPISAP = (cantidadAPInt * precioPIVA);
                //}

                //precioReal = sumaEPISN + sumaEPISAP + sumaEPISAT;

                //if (precioReal == precioIngresado)
                //{
                //    condicionante = "Aprobado";
                //}

                IVAS = "SI";
            }
            else
            {
                //if (cantidadInt != 0)
                //{
                //    sumaEPISN = (cantidadInt * precioN);
                //}
                //if (cantidadATInt != 0)
                //{
                //    sumaEPISAT = (cantidadATInt * precioAT);
                //}
                //if (cantidadAPInt != 0)
                //{
                //    sumaEPISAP = (cantidadAPInt * precioP);
                //}

                //precioReal = sumaEPISN + sumaEPISAP + sumaEPISAT;

                //if (precioReal == precioIngresado)
                //{
                //    condicionante = "Aprobado";
                //}

                IVAS = "NO";
            }

            //if (condicionante != "Aprobado")
            //{
            //    return View("PrecioIncorrecto");
            //}

            //else
            //{




            Paciente paciente1 = new Paciente();

            //var revisionPaciente = from i in db.Paciente where i.Nombre == nombre.ToUpper() select i ;

            //if(revisionPaciente != null)
            //{
            //    List<Captura> data = db.Captura.ToList();
            //    JavaScriptSerializer js = new JavaScriptSerializer();
            //    var selected = data.Where(r => r.NombrePaciente == nombre.ToUpper())
            //        .Select(S => new {
            //            idCaptura = S.idCaptura,
            //            S.NombrePaciente,
            //            S.TipoTramite,
            //            S.NoExpediente,
            //            S.Sucursal,
            //            S.Doctor,
            //            S.EstatusCaptura
            //        }).FirstOrDefault();

            //    return Json(selected, JsonRequestBehavior.AllowGet);
            //}

            int cantidadN;
            int cantidadA;
            int cantidadAP;

            if (cantidad == "")
            {
                cantidadN = 0;
            }
            else
            {
                cantidadN = Convert.ToInt32(cantidad);
            }

            if (cantidadAereo == "")
            {
                cantidadA = 0;
            }
            else
            {
                cantidadA = Convert.ToInt32(cantidadAereo);
            }

            if (cantidadPista == "")
            {
                cantidadAP = 0;
            }
            else
            {
                cantidadAP = Convert.ToInt32(cantidadPista);
            }


            if ((cantidadN + cantidadA + cantidadAP) == 1)
            {
                Paciente paciente = new Paciente();
                paciente.Nombre = nombre.ToUpper()/*.Normalize(System.Text.NormalizationForm.FormD).Replace(@"´¨", "")*/;
                paciente.Telefono = telefono;
                paciente.Email = email;

                //MACRO TABLA
                CondensadoPaciente condensadoPaciente = new CondensadoPaciente();
                condensadoPaciente.Nombre = nombre.ToUpper()/*.Normalize(System.Text.NormalizationForm.FormD).Replace(@"´¨", "")*/;
                condensadoPaciente.Telefono = telefono;
                condensadoPaciente.Email = email;

                string hash;
                do
                {
                    Random numero = new Random();
                    int randomize = numero.Next(0, 61);
                    string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                    string get_1;
                    get_1 = aleatorio[randomize];
                    hash = get_1;
                    for (int i = 0; i < 9; i++)
                    {
                        randomize = numero.Next(0, 61);
                        get_1 = aleatorio[randomize];
                        hash += get_1;
                    }
                } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                paciente.HASH = hash;

                //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                //Se obtiene el número del contador desde la base de datos
                int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                //Contadores por número de citas en cada sucursal
                string contador = "";
                if (num == null)
                {
                    contador = "100";
                }
                else if (num < 10)
                {
                    contador = "00" + Convert.ToString(num);
                }
                else if (num >= 10 && num < 100)
                {
                    contador = "0" + Convert.ToString(num);
                }
                else
                {
                    contador = Convert.ToString(num);
                }

                //Se asigna el número de ID del doctor
                //if(Convert.ToInt32(doc) < 10)
                //{
                //    doc = "0" + doc;
                //}

                string mes = DateTime.Now.Month.ToString();
                string dia = DateTime.Now.Day.ToString();
                char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                string anio = "";

                for (int i = 2; i < year.Length; i++)
                {
                    anio += year[i];
                }

                if (Convert.ToInt32(mes) < 10)
                {
                    mes = "0" + mes;
                }

                if (Convert.ToInt32(dia) < 10)
                {
                    dia = "0" + dia;
                }

                //Se crea el número de Folio
                //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                string numFolio = dia + mes + anio + SUC + "-" + contador;
                paciente.Folio = dia + mes + anio + SUC + "-" + contador;
                condensadoPaciente.Folio = dia + mes + anio + SUC + "-" + contador;

                if (ModelState.IsValid)
                {
                    db.Paciente.Add(paciente);

                    db.SaveChanges();
                    //return RedirectToAction("Index");
                }

                CarruselMedico cm = new CarruselMedico();
                cm.idPaciente = paciente.idPaciente;

                if (ModelState.IsValid)
                {
                    db.CarruselMedico.Add(cm);
                    db.SaveChanges();
                    //return RedirectToAction("Index");
                }

                int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                Sucursales suc = db.Sucursales.Find(idSuc);

                suc.Contador = Convert.ToInt32(num);

                if (ModelState.IsValid)
                {
                    db.Entry(suc).State = EntityState.Modified;
                    db.SaveChanges();
                    //No retorna ya que sigue el proceso
                    //return RedirectToAction("Index");
                }

                var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                Cita cita = new Cita();

                cita.idPaciente = idPaciente;
                cita.FechaReferencia = DateTime.Now;
                cita.Sucursal = sucursal;
                cita.FechaCita = fecha != null ? fecha : DateTime.Now;
                cita.Recepcionista = usuario;
                cita.EstatusPago = "Pagado";
                cita.Referencia = referencia;
                cita.Folio = numFolio;
                cita.Canal = "Recepción";
                cita.TipoPago = pago;
                cita.FechaCreacion = DateTime.Now;

                //MACROTABLA
                condensadoPaciente.idPaciente = idPaciente;
                condensadoPaciente.FechaReferencia = DateTime.Now;
                condensadoPaciente.Sucursal = sucursal;
                condensadoPaciente.FechaCita = fecha != null ? fecha : DateTime.Now;
                condensadoPaciente.Recepcionista = usuario;
                condensadoPaciente.EstatusPago = "Pagado";
                condensadoPaciente.Referencia = referencia;
                condensadoPaciente.Folio = numFolio;
                condensadoPaciente.Canal = "Recepción";
                condensadoPaciente.TipoPago = pago;
                condensadoPaciente.FechaCreacion = DateTime.Now;

                if (tipoGestor == "OTRO")
                {
                    var calculoIVA = 0;
                    var precioSoloEpi = 0;
                    double ventaTotal = 0;
                    double IVATotal = 0;
                    double ventaTotalIVA = 0;

                    if (IVAS == "SI")
                    {
                        if (cantidadInt != 0)
                        {
                            calculoIVA = precioNIVA - precioN;
                            precioSoloEpi = precioN;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                            condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                            condensadoPaciente.IVA = Convert.ToString(calculoIVA);
                        }
                        if (cantidadATInt != 0)
                        {
                            calculoIVA = precioATIVA - precioAT;
                            precioSoloEpi = precioAT;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                            condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                            condensadoPaciente.IVA = Convert.ToString(calculoIVA);
                        }
                        if (cantidadAPInt != 0)
                        {
                            calculoIVA = precioPIVA - precioP;
                            precioSoloEpi = precioP;
                            condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                            condensadoPaciente.IVA = Convert.ToString(calculoIVA);
                        }
                    }
                    else
                    {
                        if (cantidadInt != 0)
                        {
                            precioSoloEpi = precioN;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                        }
                        if (cantidadATInt != 0)
                        {
                            precioSoloEpi = precioN;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                        }
                        if (cantidadAPInt != 0)
                        {
                            precioSoloEpi = precioN;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                        }
                    }

                    if (cantidadATInt == 0 && cantidadAPInt == 0)
                    {
                        if (cb_Seguro != null)
                        {
                            cita.ventaSeguro = "SI";
                            cita.CostoSeguro = "100";
                            cita.IvaSeguro = "16";
                            cita.TotalSeguro = "116";
                            ventaTotal = precioSoloEpi + 100;
                            cita.TotalVenta = Convert.ToString(ventaTotal);
                            IVATotal = calculoIVA + 16;
                            cita.TotalIVA = Convert.ToString(IVATotal);
                            ventaTotalIVA = ventaTotal + IVATotal;
                            cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);

                            //MACROTABLA
                            condensadoPaciente.ventaSeguro = "SI";
                            condensadoPaciente.CostoSeguro = "100";
                            condensadoPaciente.IvaSeguro = "16";
                            condensadoPaciente.TotalSeguro = "116";
                            condensadoPaciente.TotalVenta = Convert.ToString(ventaTotal);
                            condensadoPaciente.TotalIVA = Convert.ToString(IVATotal);
                            condensadoPaciente.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                        }
                        else
                        {
                            cita.ventaSeguro = "NO";
                            cita.CostoSeguro = "32";
                            cita.IvaSeguro = "5.12";
                            cita.TotalSeguro = "37.12";
                            ventaTotal = precioSoloEpi + 32;
                            cita.TotalVenta = Convert.ToString(ventaTotal);
                            IVATotal = calculoIVA + 5.12;
                            cita.TotalIVA = Convert.ToString(IVATotal);
                            ventaTotalIVA = ventaTotal + IVATotal;
                            cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);

                            //MACROTABLA
                            condensadoPaciente.ventaSeguro = "NO";
                            condensadoPaciente.CostoSeguro = "32";
                            condensadoPaciente.IvaSeguro = "5.12";
                            condensadoPaciente.TotalSeguro = "37.12";
                            condensadoPaciente.TotalVenta = Convert.ToString(ventaTotal);
                            condensadoPaciente.TotalIVA = Convert.ToString(IVATotal);
                            condensadoPaciente.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                        }
                    }
                }

                //Se usa el idCanal para poder hacer que en Recepción se tenga que editar el nombre si viene de gestor
                cita.idCanal = 1;

                if (referido == 22)
                {
                    cita.Referencia = "E1293749";
                    condensadoPaciente.Referencia = "E1293749";
                }
                if (referido == 23)
                {
                    cita.Referencia = "PL1293750";
                    condensadoPaciente.Referencia = "PL1293750";
                }
                //if (referido == "NATALY FRANCO")
                //{
                //    cita.Referencia = "NF1293751";
                //}
                if (referido == 36)
                {
                    cita.Referencia = "LV1293752";
                    condensadoPaciente.Referencia = "LV1293752";
                }
                if (referido == 21)
                {
                    cita.Referencia = "RS1293753";
                    condensadoPaciente.Referencia = "RS1293753";
                }

                if (pago != "Referencia Scotiabank")
                {
                    var tipoPago = (from t in db.ReferenciasSB where t.ReferenciaSB == referencia select t).FirstOrDefault();

                    if (tipoPago != null)
                    {
                        ReferenciasSB refe = db.ReferenciasSB.Find(tipoPago.idReferencia);
                        refe.EstatusReferencia = "LIBRE";
                        refe.idPaciente = idPaciente;

                        if (ModelState.IsValid)
                        {
                            db.Entry(refe).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                else
                {
                    //var tipoPago = (from t in db.ReferenciasSB where t.ReferenciaSB == numero select t).FirstOrDefault();

                    var tipoPago = (from t in db.ReferenciasSB where t.ReferenciaSB == referencia select t).FirstOrDefault();

                    if (tipoPago != null)
                    {
                        ReferenciasSB refe = db.ReferenciasSB.Find(tipoPago.idReferencia);
                        refe.EstatusReferencia = "OCUPADO";

                        if (ModelState.IsValid)
                        {
                            db.Entry(refe).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                }

                string TIPOLIC = null;
                if (cantidadA != 0)
                {
                    TIPOLIC = "AEREO";
                }
                if (cantidadAP != 0)
                {
                    TIPOLIC = "AEREO_PISTA";
                }
                cita.TipoLicencia = TIPOLIC;

                //if (referido == "NINGUNO" || referido == "OTRO")
                //{
                //    cita.CC = "N/A";
                //}
                //else
                //{
                //    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                //    cita.CC = referidoTipo;
                //}

                cita.Cuenta = (pago == "Pendiente de Pago" || pago == "Credito Empresas") && (referido != 4 && referido != 5 && referido != 6) ? "CUENTAS X COBRAR" : null;
                condensadoPaciente.Cuenta = (pago == "Pendiente de Pago" || pago == "Credito Empresas") && (referido != 4 && referido != 5 && referido != 6) ? "CUENTAS X COBRAR" : null;

                var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
                cita.CC = referidoTipo.Tipo;
                cita.CanalTipo = referidoTipo.Tipo;
                condensadoPaciente.CC = referidoTipo.Tipo;
                condensadoPaciente.CanalTipo = referidoTipo.Tipo;

                cita.ReferidoPor = referidoTipo.Nombre;
                condensadoPaciente.ReferidoPor = referidoTipo.Nombre;

                if (referidoTipo.idReferido == 7 || referidoTipo.idReferido == 8 || referidoTipo.idReferido == 9 || referidoTipo.idReferido == 10 || referidoTipo.idReferido == 12 ||
                    referidoTipo.idReferido == 13 || referidoTipo.idReferido == 20 || referidoTipo.idReferido == 26 || referidoTipo.idReferido == 27 || referidoTipo.idReferido == 29 ||
                    referidoTipo.idReferido == 36 || referidoTipo.idReferido == 52 || referidoTipo.idReferido == 53 || referidoTipo.idReferido == 54 || referidoTipo.idReferido == 55 ||
                    referidoTipo.idReferido == 56 || referidoTipo.idReferido == 59 || referidoTipo.idReferido == 130 || referidoTipo.idReferido == 136 || referidoTipo.idReferido == 142)
                {
                    cita.Cuenta = "CORPORATIVO";
                    condensadoPaciente.Cuenta = "CORPORATIVO";
                }


                //-------------------------------------------------------------

                if (ModelState.IsValid)
                {
                    db.Cita.Add(cita);
                    db.SaveChanges();

                    var idCita = (from r in db.Cita where r.idPaciente == idPaciente select r.idCita).FirstOrDefault();
                    condensadoPaciente.idCita = idCita;
                    db.CondensadoPaciente.Add(condensadoPaciente);
                    db.SaveChanges();

                    return RedirectToAction("Index");


                    //OPCION CON VENTANA DE PAGO


                    //return RedirectToAction("VentanaPago", new
                    //{
                    //    precioIngresado = precioIngresado,
                    //    precioN = precioN,
                    //    precioNIVA = precioNIVA,
                    //    precioAT = precioAT,
                    //    precioATIVA = precioATIVA,
                    //    precioP = precioP,
                    //    precioPIVA = precioPIVA,
                    //    sumaGestor = precioReal,
                    //    nombreGestor = findGestor.Nombre,
                    //    sucursal = sucursal,
                    //    nombrePaciente = nombre,
                    //    emailPaciente = email,
                    //    telefonoPaciente = telefono,
                    //    fechaSolicitud = fecha,
                    //    fechaExpiracion = fechaEx,
                    //    cantidadEpis = cantidad,
                    //    cantidadEpisA = cantidadAereo,
                    //    cantidadEpisAP = cantidadAP
                    //});
                }

            }
            else
            {
                //return View(detallesOrden);
                for (int n = 1; n <= Convert.ToInt32((cantidadN + cantidadA + cantidadAP)); n++)
                {
                    Paciente paciente = new Paciente();

                    paciente.Nombre = nombre.ToUpper() + " " + n;
                    paciente.Telefono = telefono;
                    paciente.Email = email;

                    //MACRO TABLA
                    CondensadoPaciente condensadoPaciente = new CondensadoPaciente();
                    condensadoPaciente.Nombre = nombre.ToUpper()/*.Normalize(System.Text.NormalizationForm.FormD).Replace(@"´¨", "")*/;
                    condensadoPaciente.Telefono = telefono;
                    condensadoPaciente.Email = email;


                    string hash;
                    do
                    {
                        Random numero = new Random();
                        int randomize = numero.Next(0, 61);
                        string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                        string get_1;
                        get_1 = aleatorio[randomize];
                        hash = get_1;
                        for (int i = 0; i < 9; i++)
                        {
                            randomize = numero.Next(0, 61);
                            get_1 = aleatorio[randomize];
                            hash += get_1;
                        }
                    } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                    paciente.HASH = hash;

                    //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                    string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                    //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                    //Se obtiene el número del contador desde la base de datos
                    int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                    //Contadores por número de citas en cada sucursal
                    string contador = "";
                    if (num == null)
                    {
                        contador = "100";
                    }
                    else if (num < 10)
                    {
                        contador = "00" + Convert.ToString(num);
                    }
                    else if (num >= 10 && num < 100)
                    {
                        contador = "0" + Convert.ToString(num);
                    }
                    else
                    {
                        contador = Convert.ToString(num);
                    }

                    //Se asigna el número de ID del doctor
                    //if (Convert.ToInt32(doc) < 10)
                    //{
                    //    doc = "0" + doc;
                    //}

                    string mes = DateTime.Now.Month.ToString();
                    string dia = DateTime.Now.Day.ToString();
                    char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                    string anio = "";

                    for (int i = 2; i < year.Length; i++)
                    {
                        anio += year[i];
                    }

                    if (Convert.ToInt32(mes) < 10)
                    {
                        mes = "0" + mes;
                    }

                    if (Convert.ToInt32(dia) < 10)
                    {
                        dia = "0" + dia;
                    }

                    //Se crea el número de Folio
                    //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                    //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                    string numFolio = dia + mes + anio + SUC + "-" + contador;
                    paciente.Folio = dia + mes + anio + SUC + "-" + contador;

                    if (ModelState.IsValid)
                    {
                        db.Paciente.Add(paciente);
                        db.SaveChanges();
                        //return RedirectToAction("Index");
                    }



                    CarruselMedico cm = new CarruselMedico();
                    cm.idPaciente = paciente.idPaciente;

                    if (ModelState.IsValid)
                    {
                        db.CarruselMedico.Add(cm);
                        db.SaveChanges();
                        //return RedirectToAction("Index");
                    }

                    int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                    Sucursales suc = db.Sucursales.Find(idSuc);

                    suc.Contador = Convert.ToInt32(num);

                    if (ModelState.IsValid)
                    {
                        db.Entry(suc).State = EntityState.Modified;
                        db.SaveChanges();
                        //No retorna ya que sigue el proceso
                        //return RedirectToAction("Index");
                    }

                    var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                    Cita cita = new Cita();

                    cita.TipoPago = pago;
                    cita.idPaciente = idPaciente;
                    cita.FechaReferencia = DateTime.Now;
                    cita.Sucursal = sucursal;
                    cita.Recepcionista = usuario;
                    cita.EstatusPago = "Pagado";
                    cita.Folio = numFolio;
                    cita.Referencia = referencia;
                    cita.Canal = "Recepción";
                    cita.FechaCita = fecha != null ? fecha : DateTime.Now;
                    cita.FechaCreacion = DateTime.Now;

                    //MACROTABLA
                    condensadoPaciente.idPaciente = idPaciente;
                    condensadoPaciente.FechaReferencia = DateTime.Now;
                    condensadoPaciente.Sucursal = sucursal;
                    condensadoPaciente.FechaCita = fecha != null ? fecha : DateTime.Now;
                    condensadoPaciente.Recepcionista = usuario;
                    condensadoPaciente.EstatusPago = "Pagado";
                    condensadoPaciente.Referencia = referencia;
                    condensadoPaciente.Folio = numFolio;
                    condensadoPaciente.Canal = "Recepción";
                    condensadoPaciente.TipoPago = pago;
                    condensadoPaciente.FechaCreacion = DateTime.Now;


                    if (tipoGestor == "OTRO")
                    {
                        var calculoIVA = 0;
                        var precioSoloEpi = 0;
                        double ventaTotal = 0;
                        double IVATotal = 0;
                        double ventaTotalIVA = 0;
                        int sumaNA = cantidadN + cantidadA;

                        if (IVAS == "SI")
                        {
                            if (n <= cantidadN)
                            {
                                calculoIVA = precioNIVA - precioN;
                                precioSoloEpi = precioN;
                                cita.Venta = Convert.ToString(precioSoloEpi);
                                cita.IVA = Convert.ToString(calculoIVA);
                                condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                                condensadoPaciente.IVA = Convert.ToString(calculoIVA);
                            }

                            if (n > cantidadN)
                            {
                                calculoIVA = precioATIVA - precioAT;
                                precioSoloEpi = precioAT;
                                cita.Venta = Convert.ToString(precioSoloEpi);
                                cita.IVA = Convert.ToString(calculoIVA);
                                condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                                condensadoPaciente.IVA = Convert.ToString(calculoIVA);
                            }

                            if (n > sumaNA)
                            {
                                calculoIVA = precioPIVA - precioP;
                                precioSoloEpi = precioP;
                                cita.Venta = Convert.ToString(precioSoloEpi);
                                cita.IVA = Convert.ToString(calculoIVA);
                                condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                                condensadoPaciente.IVA = Convert.ToString(calculoIVA);
                            }
                        }
                        else
                        {
                            if (n <= cantidadN)
                            {
                                precioSoloEpi = precioN;
                                cita.Venta = Convert.ToString(precioSoloEpi);
                                condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                            }
                            if (n > cantidadN)
                            {
                                precioSoloEpi = precioAT;
                                cita.Venta = Convert.ToString(precioSoloEpi);
                                condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                            }
                            if (n > sumaNA)
                            {
                                precioSoloEpi = precioP;
                                cita.Venta = Convert.ToString(precioSoloEpi);
                                condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                            }
                        }

                        if (n <= cantidadN)
                        {
                            if (cb_Seguro != null)
                            {
                                cita.ventaSeguro = "SI";
                                cita.CostoSeguro = "100";
                                cita.IvaSeguro = "16";
                                cita.TotalSeguro = "116";
                                ventaTotal = precioSoloEpi + 100;
                                cita.TotalVenta = Convert.ToString(ventaTotal);
                                IVATotal = calculoIVA + 16;
                                cita.TotalIVA = Convert.ToString(IVATotal);
                                ventaTotalIVA = ventaTotal + IVATotal;
                                cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);

                                //MACROTABLA
                                condensadoPaciente.ventaSeguro = "SI";
                                condensadoPaciente.CostoSeguro = "100";
                                condensadoPaciente.IvaSeguro = "16";
                                condensadoPaciente.TotalSeguro = "116";
                                condensadoPaciente.TotalVenta = Convert.ToString(ventaTotal);
                                condensadoPaciente.TotalIVA = Convert.ToString(IVATotal);
                                condensadoPaciente.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                            }
                            else
                            {
                                cita.ventaSeguro = "NO";
                                cita.CostoSeguro = "32";
                                cita.IvaSeguro = "5.12";
                                cita.TotalSeguro = "37.12";
                                ventaTotal = precioSoloEpi + 32;
                                cita.TotalVenta = Convert.ToString(ventaTotal);
                                IVATotal = calculoIVA + 5.12;
                                cita.TotalIVA = Convert.ToString(IVATotal);
                                ventaTotalIVA = ventaTotal + IVATotal;
                                cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);

                                //MACROTABLA
                                condensadoPaciente.ventaSeguro = "NO";
                                condensadoPaciente.CostoSeguro = "32";
                                condensadoPaciente.IvaSeguro = "5.12";
                                condensadoPaciente.TotalSeguro = "37.12";
                                condensadoPaciente.TotalVenta = Convert.ToString(ventaTotal);
                                condensadoPaciente.TotalIVA = Convert.ToString(IVATotal);
                                condensadoPaciente.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                            }
                        }
                    }

                    if (referido == 22)
                    {
                        cita.Referencia = "E1293749";
                        condensadoPaciente.Referencia = "E1293749";
                    }
                    if (referido == 23)
                    {
                        cita.Referencia = "PL1293750";
                        condensadoPaciente.Referencia = "PL1293750";
                    }
                    //if (referido == "NATALY FRANCO")
                    //{
                    //    cita.Referencia = "NF1293751";
                    //}
                    if (referido == 36)
                    {
                        cita.Referencia = "LV1293752";
                        condensadoPaciente.Referencia = "LV1293752";
                    }
                    if (referido == 21)
                    {
                        cita.Referencia = "RS1293753";
                        condensadoPaciente.Referencia = "RS1293753";
                    }

                    //SEPARACION AEREOS_PISTA

                    int sumaInicialTL = cantidadN + cantidadA;

                    if (n > cantidadN)
                    {
                        cita.TipoLicencia = "AEREO";
                        condensadoPaciente.TipoLicencia = "AEREO";
                    }

                    if (n > sumaInicialTL)
                    {
                        cita.TipoLicencia = "AEREO_PISTA";
                        condensadoPaciente.TipoLicencia = "AEREO_PISTA";
                    }

                    //if (referido == "NINGUNO" || referido == "OTRO")
                    //{
                    //    cita.CC = "N/A";
                    //}
                    //else
                    //{
                    //    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                    //    cita.CC = referidoTipo;
                    //}

                    cita.Cuenta = (pago == "Pendiente de Pago" || pago == "Credito Empresas") ? "CUENTAS X COBRAR" : null;
                    condensadoPaciente.Cuenta = (pago == "Pendiente de Pago" || pago == "Credito Empresas") ? "CUENTAS X COBRAR" : null;

                    var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
                    cita.CC = referidoTipo.Tipo;
                    cita.CanalTipo = referidoTipo.Tipo;
                    condensadoPaciente.CC = referidoTipo.Tipo;
                    condensadoPaciente.CanalTipo = referidoTipo.Tipo;

                    cita.ReferidoPor = referidoTipo.Nombre;
                    condensadoPaciente.ReferidoPor = referidoTipo.Nombre;

                    if (referidoTipo.idReferido == 7 || referidoTipo.idReferido == 8 || referidoTipo.idReferido == 9 || referidoTipo.idReferido == 10 || referidoTipo.idReferido == 12 ||
                    referidoTipo.idReferido == 13 || referidoTipo.idReferido == 20 || referidoTipo.idReferido == 26 || referidoTipo.idReferido == 27 || referidoTipo.idReferido == 29 ||
                    referidoTipo.idReferido == 36 || referidoTipo.idReferido == 52 || referidoTipo.idReferido == 53 || referidoTipo.idReferido == 54 || referidoTipo.idReferido == 55 ||
                    referidoTipo.idReferido == 56 || referidoTipo.idReferido == 59 || referidoTipo.idReferido == 130 || referidoTipo.idReferido == 136 || referidoTipo.idReferido == 142)
                    {
                        cita.Cuenta = "CORPORATIVO";
                        condensadoPaciente.Cuenta = "CORPORATIVO";
                    }

                    if (ModelState.IsValid)
                    {
                        db.Cita.Add(cita);
                        db.SaveChanges();
                        //no regresa ya que se debe ver la pantalla de Orden
                        //return RedirectToAction("Index");

                        var idCita = (from r in db.Cita where r.idPaciente == idPaciente select r.idCita).FirstOrDefault();
                        condensadoPaciente.idCita = idCita;
                        db.CondensadoPaciente.Add(condensadoPaciente);
                        db.SaveChanges();
                    }
                }
            }

            return RedirectToAction("Index");


            //OPCION CON VENTANA DE PAGO

            //return RedirectToAction("VentanaPago", new
            //    {
            //        precioIngresado = precioIngresado,
            //        precioN = precioN,
            //        precioNIVA = precioNIVA,
            //        precioAT = precioAT,
            //        precioATIVA = precioATIVA,
            //        precioP = precioP,
            //        precioPIVA = precioPIVA,
            //        sumaGestor = precioReal,
            //        nombreGestor = findGestor.Nombre,
            //        sucursal = sucursal,
            //        nombrePaciente = nombre,
            //        emailPaciente = email,
            //        telefonoPaciente = telefono,
            //        fechaSolicitud = fecha,
            //        fechaExpiracion = fechaEx,
            //        cantidadEpis = cantidad,
            //        cantidadEpisA = cantidadAereo,
            //        cantidadEpisAP = cantidadAP
            //    });

        }
        //}

        public ActionResult OrdenSAM(int? id)
        {
            ViewBag.idPaciente = id;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Orden(string nombre, string telefono, string email, string usuario, string sucursal, string cantidad, string cantidadAereo,
            string cantidadPista, string checkbox, int? referido, DateTime? fecha, string cb_Seguro)
        {
            GetApiKey();

            var findGestor = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
            var precioN = Convert.ToInt32(findGestor.PrecioNormal);
            var precioNIVA = Convert.ToInt32(findGestor.PrecioNormalconIVA);
            var precioAT = Convert.ToInt32(findGestor.PrecioAereosinIVA);
            var precioATIVA = Convert.ToInt32(findGestor.PrecioAereo);
            var precioP = Convert.ToInt32(findGestor.PrecioAereoPista);
            var precioPIVA = Convert.ToInt32(findGestor.PrecioAereoPistaconIVA);
            var cantidadInt = cantidad != "" ? Convert.ToInt32(cantidad) : 0;
            var cantidadATInt = cantidadAereo != "" ? Convert.ToInt32(cantidadAereo) : 0;
            var cantidadAPInt = cantidadPista != "" ? Convert.ToInt32(cantidadPista) : 0;
            var tipoGestor = findGestor.Tipo;

            string mailSeteado = "referenciasoxxo@medicinagmi.mx";

            int cantidadN;
            int cantidadA;
            int precio = 0;
            int cantidadAP;

            if (cantidad == "")
            {
                cantidadN = 0;
            }
            else
            {
                cantidadN = Convert.ToInt32(cantidad);
            }

            if (cantidadAereo == "")
            {
                cantidadA = 0;
            }
            else
            {
                cantidadA = Convert.ToInt32(cantidadAereo);
            }

            if (cantidadPista == "")
            {
                cantidadAP = 0;
            }
            else
            {
                cantidadAP = Convert.ToInt32(cantidadPista);
            }


            var episTotalesJ = cantidadN + cantidadA + cantidadAP;


            //PRECIOS POR GESTOR CMABIANDO CONEKTA

            var tomarPrecio = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();

            precio = (cantidadN * Convert.ToInt32(tomarPrecio.PrecioNormalconIVA)) + (cantidadA * Convert.ToInt32(tomarPrecio.PrecioAereo))
                + (cantidadAP * Convert.ToInt32(tomarPrecio.PrecioAereoPistaconIVA));

            //PRECIOS SIN CMABIAR CONEKTA
            //if (referido == 167)
            //{
            //    precio = (cantidadN * 2400) + (cantidadA * 3500) + (cantidadAP * 3200);
            //}

            //else if (referido == 168)
            //{
            //    precio = (cantidadN * 2784) + (cantidadA * 3500) + (cantidadAP * 3200);
            //}

            //else
            //{
            //    precio = (cantidadN * 2784) + (cantidadA * 4060) + (cantidadAP * 3712);
            //}

            if (precio > 10000)
            {
                precio = 9990;
            }

            if (cantidadAereo == "" && cantidad == "" && cantidadPista == "")
            {
                return View("Index");
            }


            Order order = new conekta.Order().create(@"{
                      ""currency"":""MXN"",
                      ""customer_info"": " + ConvertirCliente(nombre, mailSeteado, telefono) + @",
                      ""line_items"": [{
                      ""name"": " + ConvertirProductos1(sucursal) + @",
                      ""unit_price"": " + ConvertirProductos2(Convert.ToString(precio)) + @",
                      ""quantity"": 1
                      }]
                      }");

            order.createCharge(@"{
                    ""payment_method"": {
                    ""type"": ""oxxo_cash""
                    },
                    ""amount"": " + ConvertirProductos2(Convert.ToString(precio)) + @"
                    }");

            var orden = new Order().find(order.id);

            var detallesOrden = new Order()
            {
                id = orden.id,
                customer_info = orden.customer_info,
                line_items = orden.line_items,
                amount = orden.amount,
                charges = orden.charges
            };

            var referenciaSB = (from r in db.ReferenciasSB where r.EstatusReferencia == "LIBRE" select r.ReferenciaSB).FirstOrDefault();
            ViewBag.ReferenciaSB = referenciaSB;

            ViewBag.Orden = order.id;
            ViewBag.Metodo = "OXXO";

            if ((cantidadN + cantidadA + cantidadAP) == 1)
            {
                Paciente paciente = new Paciente();

                paciente.Nombre = nombre.ToUpper();
                paciente.Telefono = telefono;
                paciente.Email = email;

                //MACRO TABLA
                CondensadoPaciente condensadoPaciente = new CondensadoPaciente();
                condensadoPaciente.Nombre = nombre.ToUpper()/*.Normalize(System.Text.NormalizationForm.FormD).Replace(@"´¨", "")*/;
                condensadoPaciente.Telefono = telefono;
                condensadoPaciente.Email = email;

                string hash;
                do
                {
                    Random numero = new Random();
                    int randomize = numero.Next(0, 61);
                    string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                    string get_1;
                    get_1 = aleatorio[randomize];
                    hash = get_1;
                    for (int i = 0; i < 9; i++)
                    {
                        randomize = numero.Next(0, 61);
                        get_1 = aleatorio[randomize];
                        hash += get_1;
                    }
                } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                paciente.HASH = hash;


                //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                //Se obtiene el número del contador desde la base de datos
                int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                //Contadores por número de citas en cada sucursal
                string contador = "";
                if (num == null)
                {
                    contador = "100";
                }
                else if (num < 10)
                {
                    contador = "00" + Convert.ToString(num);
                }
                else if (num >= 10 && num < 100)
                {
                    contador = "0" + Convert.ToString(num);
                }
                else
                {
                    contador = Convert.ToString(num);
                }

                //Se asigna el número de ID del doctor
                //if (Convert.ToInt32(doc) < 10)
                //{
                //    doc = "0" + doc;
                //}

                string mes = DateTime.Now.Month.ToString();
                string dia = DateTime.Now.Day.ToString();
                char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                string anio = "";

                for (int i = 2; i < year.Length; i++)
                {
                    anio += year[i];
                }

                if (Convert.ToInt32(mes) < 10)
                {
                    mes = "0" + mes;
                }

                if (Convert.ToInt32(dia) < 10)
                {
                    dia = "0" + dia;
                }

                //Se crea el número de Folio
                //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                string numFolio = dia + mes + anio + SUC + "-" + contador;
                paciente.Folio = dia + mes + anio + SUC + "-" + contador;
                condensadoPaciente.Folio = dia + mes + anio + SUC + "-" + contador;

                if (ModelState.IsValid)
                {
                    db.Paciente.Add(paciente);
                    db.SaveChanges();
                    //return RedirectToAction("Index");
                }

                int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                Sucursales suc = db.Sucursales.Find(idSuc);

                suc.Contador = Convert.ToInt32(num);

                if (ModelState.IsValid)
                {
                    db.Entry(suc).State = EntityState.Modified;
                    db.SaveChanges();
                    //No retorna ya que sigue el proceso
                    //return RedirectToAction("Index");
                }

                var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                Cita cita = new Cita();

                cita.TipoPago = "Referencia OXXO";
                cita.NoOrden = orden.id;
                condensadoPaciente.TipoPago = "Referencia OXXO";
                condensadoPaciente.NoOrden = orden.id;


                JavaScriptSerializer js = new JavaScriptSerializer();
                dynamic datosCargo2 = js.Deserialize<dynamic>(orden.charges.data[0].ToString());

                string referencia = datosCargo2["payment_method"]["reference"].ToString();

                cita.Referencia = referencia;
                cita.idPaciente = idPaciente;
                cita.FechaReferencia = DateTime.Now;
                cita.Sucursal = sucursal;
                cita.Recepcionista = usuario;
                cita.EstatusPago = orden.payment_status;
                cita.Folio = numFolio;
                cita.Canal = "Recepción";
                cita.FechaCita = fecha != null ? fecha : DateTime.Now;
                cita.FechaCreacion = DateTime.Now;

                //MACROTABLA
                condensadoPaciente.idPaciente = idPaciente;
                condensadoPaciente.FechaReferencia = DateTime.Now;
                condensadoPaciente.Sucursal = sucursal;
                condensadoPaciente.FechaCita = fecha != null ? fecha : DateTime.Now;
                condensadoPaciente.Recepcionista = usuario;
                condensadoPaciente.EstatusPago = "Pagado";
                condensadoPaciente.Referencia = referencia;
                condensadoPaciente.Folio = numFolio;
                condensadoPaciente.Canal = "Recepción";
                condensadoPaciente.FechaCreacion = DateTime.Now;

                if (cantidadATInt == 0 && cantidadAPInt == 0)
                {

                    if (tipoGestor == "OTRO")
                    {
                        double calculoIVA = 0;
                        var precioSoloEpi = (precio / 1.16);
                        double ventaTotal = 0;
                        double IVATotal = 0;
                        double ventaTotalIVA = 0;

                        calculoIVA = precio - (precio / 1.16);
                        precioSoloEpi = (precio / 1.16);
                        cita.Venta = Convert.ToString(precioSoloEpi);
                        cita.IVA = Convert.ToString(calculoIVA);
                        condensadoPaciente.Venta = Convert.ToString(precioSoloEpi);
                        condensadoPaciente.IVA = Convert.ToString(calculoIVA);

                        if (cb_Seguro != null)
                        {
                            cita.ventaSeguro = "SI";
                            cita.CostoSeguro = "100";
                            cita.IvaSeguro = "16";
                            cita.TotalSeguro = "116";
                            ventaTotal = precioSoloEpi + 100;
                            cita.TotalVenta = Convert.ToString(ventaTotal);
                            IVATotal = calculoIVA + 16;
                            cita.TotalIVA = Convert.ToString(IVATotal);
                            ventaTotalIVA = ventaTotal + IVATotal;
                            cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);

                            //MACROTABLA
                            condensadoPaciente.ventaSeguro = "SI";
                            condensadoPaciente.CostoSeguro = "100";
                            condensadoPaciente.IvaSeguro = "16";
                            condensadoPaciente.TotalSeguro = "116";
                            condensadoPaciente.TotalVenta = Convert.ToString(ventaTotal);
                            condensadoPaciente.TotalIVA = Convert.ToString(IVATotal);
                            condensadoPaciente.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                        }
                        else
                        {
                            cita.ventaSeguro = "NO";
                            cita.CostoSeguro = "32";
                            cita.IvaSeguro = "5.12";
                            cita.TotalSeguro = "37.12";
                            ventaTotal = precioSoloEpi + 32;
                            cita.TotalVenta = Convert.ToString(ventaTotal);
                            IVATotal = calculoIVA + 5.12;
                            cita.TotalIVA = Convert.ToString(IVATotal);
                            ventaTotalIVA = ventaTotal + IVATotal;
                            cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);

                            //MACROTABLA
                            condensadoPaciente.ventaSeguro = "NO";
                            condensadoPaciente.CostoSeguro = "32";
                            condensadoPaciente.IvaSeguro = "5.12";
                            condensadoPaciente.TotalSeguro = "37.12";
                            condensadoPaciente.TotalVenta = Convert.ToString(ventaTotal);
                            condensadoPaciente.TotalIVA = Convert.ToString(IVATotal);
                            condensadoPaciente.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                        }
                    }
                }

                //Se usa el idCanal para poder hacer que en Recepción se tenga que editar el nombre si viene de gestor
                cita.idCanal = 1;

                //if (referido == 22)
                //{
                //    cita.Referencia = "E1293749";
                //}
                //if (referido == 23)
                //{
                //    cita.Referencia = "PL1293750";
                //}
                ////if (referido == "NATALY FRANCO")
                ////{
                ////    cita.Referencia = "NF1293751";
                ////}
                //if (referido == 36)
                //{
                //    cita.Referencia = "LV1293752";
                //}
                //if (referido == 21)
                //{
                //    cita.Referencia = "RS1293753";
                //}

                int idRefSB = Convert.ToInt32((from r in db.ReferenciasSB where r.ReferenciaSB == referenciaSB select r.idReferencia).FirstOrDefault());
                ReferenciasSB refe = db.ReferenciasSB.Find(idRefSB);
                refe.EstatusReferencia = "PENDIENTE";
                refe.idPaciente = idPaciente;

                string TIPOLIC = null;
                if (cantidadA != 0)
                {
                    TIPOLIC = "AEREO";
                }
                if (cantidadAP != 0)
                {
                    TIPOLIC = "AEREO_PISTA";
                }
                cita.TipoLicencia = TIPOLIC;
                condensadoPaciente.TipoLicencia = TIPOLIC;

                //if (referido == "NINGUNO" || referido == "OTRO")
                //{
                //    cita.CC = "N/A";
                //}
                //else
                //{
                //    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                //    cita.CC = referidoTipo;
                //}

                var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
                cita.CC = referidoTipo.Tipo;
                cita.CanalTipo = referidoTipo.Tipo;
                condensadoPaciente.CC = referidoTipo.Tipo;
                condensadoPaciente.CanalTipo = referidoTipo.Tipo;

                cita.ReferidoPor = referidoTipo.Nombre;
                condensadoPaciente.ReferidoPor = referidoTipo.Nombre;

                if (referidoTipo.idReferido == 7 || referidoTipo.idReferido == 8 || referidoTipo.idReferido == 9 || referidoTipo.idReferido == 10 || referidoTipo.idReferido == 12 ||
                    referidoTipo.idReferido == 13 || referidoTipo.idReferido == 20 || referidoTipo.idReferido == 26 || referidoTipo.idReferido == 27 || referidoTipo.idReferido == 29 ||
                    referidoTipo.idReferido == 36 || referidoTipo.idReferido == 52 || referidoTipo.idReferido == 53 || referidoTipo.idReferido == 54 || referidoTipo.idReferido == 55 ||
                    referidoTipo.idReferido == 56 || referidoTipo.idReferido == 59 || referidoTipo.idReferido == 130 || referidoTipo.idReferido == 136 || referidoTipo.idReferido == 142)
                {
                    cita.Cuenta = "CORPORATIVO";
                    condensadoPaciente.Cuenta = "CORPORATIVO";
                }

                if (ModelState.IsValid)
                {
                    db.Cita.Add(cita);

                    var idCita = (from r in db.Cita where r.idPaciente == idPaciente select r.idCita).FirstOrDefault();
                    condensadoPaciente.idCita = idCita;

                    db.CondensadoPaciente.Add(condensadoPaciente);
                    db.Entry(refe).State = EntityState.Modified;
                    db.SaveChanges();
                    //no regresa ya que se debe ver la pantalla de Orden
                    //return RedirectToAction("Index");
                }
            }
            else
            {
                //return View(detallesOrden);
                for (int n = 1; n <= Convert.ToInt32((cantidadN + cantidadA + cantidadAP)); n++)
                {
                    Paciente paciente = new Paciente();
                    CondensadoPaciente condensadoPaciente = new CondensadoPaciente();

                    paciente.Nombre = nombre.ToUpper() + " " + n;
                    paciente.Telefono = telefono;
                    paciente.Email = email;

                    string hash;
                    do
                    {
                        Random numero = new Random();
                        int randomize = numero.Next(0, 61);
                        string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                        string get_1;
                        get_1 = aleatorio[randomize];
                        hash = get_1;
                        for (int i = 0; i < 9; i++)
                        {
                            randomize = numero.Next(0, 61);
                            get_1 = aleatorio[randomize];
                            hash += get_1;
                        }
                    } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                    paciente.HASH = hash;


                    //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                    string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                    //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                    //Se obtiene el número del contador desde la base de datos
                    int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                    //Contadores por número de citas en cada sucursal
                    string contador = "";
                    if (num == null)
                    {
                        contador = "100";
                    }
                    else if (num < 10)
                    {
                        contador = "00" + Convert.ToString(num);
                    }
                    else if (num >= 10 && num < 100)
                    {
                        contador = "0" + Convert.ToString(num);
                    }
                    else
                    {
                        contador = Convert.ToString(num);
                    }

                    //Se asigna el número de ID del doctor
                    //if (Convert.ToInt32(doc) < 10)
                    //{
                    //    doc = "0" + doc;
                    //}

                    string mes = DateTime.Now.Month.ToString();
                    string dia = DateTime.Now.Day.ToString();
                    char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                    string anio = "";

                    for (int i = 2; i < year.Length; i++)
                    {
                        anio += year[i];
                    }

                    if (Convert.ToInt32(mes) < 10)
                    {
                        mes = "0" + mes;
                    }

                    if (Convert.ToInt32(dia) < 10)
                    {
                        dia = "0" + dia;
                    }

                    //Se crea el número de Folio
                    //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                    //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                    string numFolio = dia + mes + anio + SUC + "-" + contador;
                    paciente.Folio = dia + mes + anio + SUC + "-" + contador;

                    if (ModelState.IsValid)
                    {
                        db.Paciente.Add(paciente);
                        db.SaveChanges();
                        //return RedirectToAction("Index");
                    }

                    int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                    Sucursales suc = db.Sucursales.Find(idSuc);

                    suc.Contador = Convert.ToInt32(num);

                    if (ModelState.IsValid)
                    {
                        db.Entry(suc).State = EntityState.Modified;
                        db.SaveChanges();
                        //No retorna ya que sigue el proceso
                        //return RedirectToAction("Index");
                    }

                    var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                    Cita cita = new Cita();

                    cita.TipoPago = "Referencia OXXO";
                    cita.NoOrden = orden.id;


                    JavaScriptSerializer js = new JavaScriptSerializer();
                    dynamic datosCargo2 = js.Deserialize<dynamic>(orden.charges.data[0].ToString());

                    string referencia = datosCargo2["payment_method"]["reference"].ToString();

                    cita.Referencia = referencia;
                    cita.idPaciente = idPaciente;
                    cita.FechaReferencia = DateTime.Now;
                    cita.Sucursal = sucursal;
                    cita.Recepcionista = usuario;
                    cita.EstatusPago = orden.payment_status;
                    cita.Folio = numFolio;
                    cita.Canal = "Recepción";
                    cita.FechaCita = fecha != null ? fecha : DateTime.Now;
                    cita.FechaCreacion = DateTime.Now;

                    if (tipoGestor == "OTRO")
                    {
                        double calculoIVA = 0;
                        double precioSoloEpi = 0;
                        double ventaTotal = 0;
                        double IVATotal = 0;
                        double ventaTotalIVA = 0;
                        int sumaNA = cantidadN + cantidadA;
                        double preciosinIVA = precio / 1.16;

                        if (n <= cantidadN)
                        {
                            calculoIVA = precioNIVA - precioN;
                            precioSoloEpi = precioN;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                        }

                        if (n > cantidadN)
                        {
                            calculoIVA = precioATIVA - precioAT;
                            precioSoloEpi = precioAT;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                        }

                        if (n > sumaNA)
                        {
                            calculoIVA = precioPIVA - precioP;
                            precioSoloEpi = precioP;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                        }

                        if (n <= cantidadN)
                        {
                            if (cb_Seguro != null)
                            {
                                cita.ventaSeguro = "SI";
                                cita.CostoSeguro = "100";
                                cita.IvaSeguro = "16";
                                cita.TotalSeguro = "116";
                                ventaTotal = precioSoloEpi + 100;
                                cita.TotalVenta = Convert.ToString(ventaTotal);
                                IVATotal = calculoIVA + 16;
                                cita.TotalIVA = Convert.ToString(IVATotal);
                                ventaTotalIVA = ventaTotal + IVATotal;
                                cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                            }
                            else
                            {
                                cita.ventaSeguro = "NO";
                                cita.CostoSeguro = "32";
                                cita.IvaSeguro = "5.12";
                                cita.TotalSeguro = "37.12";
                                ventaTotal = precioSoloEpi + 32;
                                cita.TotalVenta = Convert.ToString(ventaTotal);
                                IVATotal = calculoIVA + 5.12;
                                cita.TotalIVA = Convert.ToString(IVATotal);
                                ventaTotalIVA = ventaTotal + IVATotal;
                                cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                            }
                        }
                    }


                    if (referido == 22)
                    {
                        cita.Referencia = "E1293749";
                    }
                    if (referido == 23)
                    {
                        cita.Referencia = "PL1293750";
                    }
                    //if (referido == "NATALY FRANCO")
                    //{
                    //    cita.Referencia = "NF1293751";
                    //}
                    if (referido == 36)
                    {
                        cita.Referencia = "LV1293752";
                    }
                    if (referido == 21)
                    {
                        cita.Referencia = "RS1293753";
                    }

                    //SEPARACION AEREOS_PISTA
                    int sumaInicialTL = cantidadN + cantidadA;

                    if (n > cantidadN)
                    {
                        cita.TipoLicencia = "AEREO";
                    }

                    if (n > sumaInicialTL)
                    {
                        cita.TipoLicencia = "AEREO_PISTA";
                    }

                    int idRefSB = Convert.ToInt32((from r in db.ReferenciasSB where r.ReferenciaSB == referenciaSB select r.idReferencia).FirstOrDefault());
                    ReferenciasSB refe = db.ReferenciasSB.Find(idRefSB);
                    refe.EstatusReferencia = "PENDIENTE";
                    refe.idPaciente = idPaciente;

                    //if (referido == "NINGUNO" || referido == "OTRO")
                    //{
                    //    cita.CC = "N/A";
                    //}
                    //else
                    //{
                    //    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                    //    cita.CC = referidoTipo;
                    //}

                    var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
                    cita.CC = referidoTipo.Tipo;
                    cita.CanalTipo = referidoTipo.Tipo;

                    cita.ReferidoPor = referidoTipo.Nombre;

                    if (referidoTipo.idReferido == 7 || referidoTipo.idReferido == 8 || referidoTipo.idReferido == 9 || referidoTipo.idReferido == 10 || referidoTipo.idReferido == 12 ||
                    referidoTipo.idReferido == 13 || referidoTipo.idReferido == 20 || referidoTipo.idReferido == 26 || referidoTipo.idReferido == 27 || referidoTipo.idReferido == 29 ||
                    referidoTipo.idReferido == 36 || referidoTipo.idReferido == 52 || referidoTipo.idReferido == 53 || referidoTipo.idReferido == 54 || referidoTipo.idReferido == 55 ||
                    referidoTipo.idReferido == 56 || referidoTipo.idReferido == 59 || referidoTipo.idReferido == 130 || referidoTipo.idReferido == 136 || referidoTipo.idReferido == 142)
                    {
                        cita.Cuenta = "CORPORATIVO";
                    }

                    if (ModelState.IsValid)
                    {
                        db.Entry(refe).State = EntityState.Modified;
                        db.Cita.Add(cita);
                        db.SaveChanges();
                        //no regresa ya que se debe ver la pantalla de Orden
                        //return RedirectToAction("Index");
                    }

                    var tablaCita = (from r in db.Cita where r.idPaciente == idPaciente select r).FirstOrDefault();
                    var tablaPaciente = (from w in db.Paciente where w.idPaciente == idPaciente select w).FirstOrDefault();

                    condensadoPaciente.Nombre = tablaPaciente.Nombre;
                    condensadoPaciente.Telefono = tablaPaciente.Telefono;
                    condensadoPaciente.Email = tablaPaciente.Email;
                    condensadoPaciente.Folio = tablaPaciente.Folio;
                    condensadoPaciente.idCita = tablaCita.idCita;
                    condensadoPaciente.TipoPago = tablaCita.TipoPago;
                    condensadoPaciente.FechaReferencia = tablaCita.FechaReferencia;
                    condensadoPaciente.NoOrden = tablaCita.NoOrden;
                    condensadoPaciente.EstatusPago = tablaCita.EstatusPago;
                    condensadoPaciente.TipoLicencia = tablaCita.TipoLicencia;
                    condensadoPaciente.NoExpediente = tablaCita.NoExpediente;
                    condensadoPaciente.FechaCita = tablaCita.FechaCita;
                    condensadoPaciente.Sucursal = tablaCita.Sucursal;
                    condensadoPaciente.Doctor = tablaCita.Doctor;
                    condensadoPaciente.Recepcionista = tablaCita.Recepcionista;
                    condensadoPaciente.idPaciente = tablaCita.idPaciente;
                    condensadoPaciente.TipoTramite = tablaCita.TipoTramite;
                    condensadoPaciente.Referencia = tablaCita.Referencia;
                    condensadoPaciente.Canal = tablaCita.Canal;
                    condensadoPaciente.CC = tablaCita.CC;
                    condensadoPaciente.Asistencia = tablaCita.Asistencia;
                    condensadoPaciente.CancelaComentario = tablaCita.CancelaComentario;
                    condensadoPaciente.Entregado = tablaCita.Entregado;
                    condensadoPaciente.ReferidoPor = tablaCita.Referencia;
                    condensadoPaciente.FechaCreacion = tablaCita.FechaCreacion;
                    condensadoPaciente.Cuenta = tablaCita.Cuenta;
                    condensadoPaciente.CanalTipo = tablaCita.CanalTipo;
                    condensadoPaciente.CuentaComentario = tablaCita.CuentaComentario;
                    condensadoPaciente.Conciliado = tablaCita.Conciliado;
                    condensadoPaciente.FechaContable = tablaCita.FechaContable;
                    condensadoPaciente.UsarSaldo = tablaCita.UsarSaldo;
                    condensadoPaciente.FormaPago = tablaCita.FormaPago;
                    condensadoPaciente.PrecioEpi = tablaCita.PrecioEpi;
                    condensadoPaciente.ConciliarPago = tablaCita.ConciliarPago;
                    condensadoPaciente.RastreoPagos = tablaCita.RastreoPagos;
                    condensadoPaciente.Venta = tablaCita.Venta;
                    condensadoPaciente.IVA = tablaCita.IVA;
                    condensadoPaciente.CostoSeguro = tablaCita.CostoSeguro;
                    condensadoPaciente.IvaSeguro = tablaCita.IvaSeguro;
                    condensadoPaciente.TotalSeguro = tablaCita.TotalSeguro;
                    condensadoPaciente.TotalVenta = tablaCita.TotalVenta;
                    condensadoPaciente.ventaSeguro = tablaCita.ventaSeguro;
                    condensadoPaciente.TotalIVA = tablaCita.TotalIVA;
                    condensadoPaciente.TotalVentaIVA = tablaCita.TotalVentaIVA;

                    if (ModelState.IsValid)
                    {
                        db.CondensadoPaciente.Add(condensadoPaciente);
                        db.SaveChanges();
                    }
                }
            }

            QRCodeEncoder encoder = new QRCodeEncoder();
            Bitmap img = encoder.Encode("sdfsdf");
            System.Drawing.Image QR = (System.Drawing.Image)img;

            byte[] imageBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                QR.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                imageBytes = ms.ToArray();
            }

            ViewBag.QR = imageBytes;

            ViewBag.AEREO = Convert.ToInt32(cantidadA) + Convert.ToInt32(cantidadAP);
            ViewBag.AUTO = Convert.ToInt32(cantidadN);

            //PRECIO ALTERANDO CONEKTA

            int putaMadre = 0;
            var laRePutaMadre = 0;

            if (tipoGestor == "OTRO")
            {
                if (cb_Seguro != null)
                {
                    if (cantidadInt == 0)
                    {
                        putaMadre = (Convert.ToInt32(cantidadN) * Convert.ToInt32(tomarPrecio.PrecioNormalconIVA))
                    + (Convert.ToInt32(cantidadA) * Convert.ToInt32(tomarPrecio.PrecioAereo))
                    + (Convert.ToInt32(cantidadAP) * Convert.ToInt32(tomarPrecio.PrecioAereoPistaconIVA));
                    }

                    else
                    {
                        laRePutaMadre = cantidadInt * 116;

                        putaMadre = (Convert.ToInt32(cantidadN) * Convert.ToInt32(tomarPrecio.PrecioNormalconIVA))
                           + (Convert.ToInt32(cantidadA) * Convert.ToInt32(tomarPrecio.PrecioAereo))
                           + (Convert.ToInt32(cantidadAP) * Convert.ToInt32(tomarPrecio.PrecioAereoPistaconIVA))
                           + laRePutaMadre;
                    }

                }

                else
                {
                    putaMadre = (Convert.ToInt32(cantidadN) * Convert.ToInt32(tomarPrecio.PrecioNormalconIVA))
                    + (Convert.ToInt32(cantidadA) * Convert.ToInt32(tomarPrecio.PrecioAereo))
                    + (Convert.ToInt32(cantidadAP) * Convert.ToInt32(tomarPrecio.PrecioAereoPistaconIVA));
                }
            }

            else
            {
                putaMadre = (Convert.ToInt32(cantidadN) * Convert.ToInt32(tomarPrecio.PrecioNormalconIVA))
                    + (Convert.ToInt32(cantidadA) * Convert.ToInt32(tomarPrecio.PrecioAereo))
                    + (Convert.ToInt32(cantidadAP) * Convert.ToInt32(tomarPrecio.PrecioAereoPistaconIVA));
            }

            ViewBag.Precio = putaMadre;

            return View(detallesOrden);
        }

        public ActionResult AbrirEPI(int? id, DateTime fechaInicio, DateTime fechaFinal)
        {
            var expediente = (from e in db.Expedientes where e.idPaciente == id select e).FirstOrDefault();

            if (id != null)
            {
                TempData["ID"] = id;
                return RedirectToAction("Index", new { inicio = fechaInicio, final = fechaFinal });
            }
            else
            {
                TempData["ID"] = null;
                return RedirectToAction("Index", new { inicio = fechaInicio, final = fechaFinal });
            }
        }

        public ActionResult AbrirEPIVentana(int id)
        {
            var expediente = (from e in db.Expedientes where e.idPaciente == id select e).FirstOrDefault();

            var bytesBinary = expediente.Expediente;
            TempData["ID"] = null;
            return File(bytesBinary, "application/pdf");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PagoTarjeta(string nombre, string telefono, string email, string usuario, string sucursal, string cantidad, string cantidadAereo, string cantidadPista, int card, int? referido, DateTime? fecha, string cb_Seguro)
        {
            GetApiKey();

            var findGestor = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
            var precioN = Convert.ToInt32(findGestor.PrecioNormal);
            var precioNIVA = Convert.ToInt32(findGestor.PrecioNormalconIVA);
            var precioAT = Convert.ToInt32(findGestor.PrecioAereosinIVA);
            var precioATIVA = Convert.ToInt32(findGestor.PrecioAereo);
            var precioP = Convert.ToInt32(findGestor.PrecioAereoPista);
            var precioPIVA = Convert.ToInt32(findGestor.PrecioAereoPistaconIVA);
            var cantidadInt = cantidad != "" ? Convert.ToInt32(cantidad) : 0;
            var cantidadATInt = cantidadAereo != "" ? Convert.ToInt32(cantidadAereo) : 0;
            var cantidadAPInt = cantidadPista != "" ? Convert.ToInt32(cantidadPista) : 0;
            var tipoGestor = findGestor.Tipo;

            int cantidadN;
            int cantidadA;
            int precio = 0;
            int cantidadAP;

            if (cantidad == "")
            {
                cantidadN = 0;
            }
            else
            {
                cantidadN = Convert.ToInt32(cantidad);
            }

            if (cantidadAereo == "")
            {
                cantidadA = 0;
            }
            else
            {
                cantidadA = Convert.ToInt32(cantidadAereo);
            }

            if (cantidadPista == "")
            {
                cantidadAP = 0;
            }
            else
            {
                cantidadAP = Convert.ToInt32(cantidadPista);
            }

            //PRECIOS POR GESTOR CAMBIANDO CONEKTA

            var tomarPrecio = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();

            precio = (cantidadN * Convert.ToInt32(tomarPrecio.PrecioNormalconIVA)) + (cantidadA * Convert.ToInt32(tomarPrecio.PrecioAereo))
                + (cantidadAP * Convert.ToInt32(tomarPrecio.PrecioAereoPistaconIVA));

            var ALV = precio;

            if (cantidadInt != 0)
            {
                if (cb_Seguro != null)
                {
                    var mamalona = cantidadInt * 116;
                    precio = precio + mamalona;
                }
            }

            //if (referido == 167)
            //{
            //    precio = (cantidadN * 2400) + (cantidadA * 3500) + (cantidadAP * 3200);
            //}

            //else if (referido == 168)
            //{
            //    precio = (cantidadN * 2784) + (cantidadA * 3500) + (cantidadAP * 3200);
            //}

            //else
            //{
            //    precio = (cantidadN * 2784) + (cantidadA * 4060) + (cantidadAP * 3712);
            //}

            if (precio > 10000)
            {
                precio = 9990;
            }

            PaymentLink nOrder = new PaymentLink().create(@"{
                      ""name"":""Pago con Tarjeta"",
                      ""type"":""PaymentLink"",
                      ""recurrent"": false,
                      ""expired_at"": " + FechaExpira() + @",
                      ""allowed_payment_methods"": [""card""],
                      ""needs_shipping_contact"": false ,
                      ""monthly_installments_enabled"": false ,
                       
                      ""order_template"": {
                      ""line_items"": [{
                      ""name"": ""Checkup EPI"",
                      ""unit_price"": " + ConvertirProductos2(Convert.ToString(precio)) + @",
                      ""quantity"": 1
                      }],

                      ""currency"":""MXN"",
                      ""metadata"":{},
                      ""customer_info"": " + ConvertirCliente(nombre, email, telefono) + @"
                      }
                    }");

            string link = nOrder.url;
            string IDEE = nOrder.id;
            TempData["Link"] = link;

            if ((cantidadN + cantidadA + cantidadAP) == 1)
            {
                Paciente paciente = new Paciente();

                paciente.Nombre = nombre.ToUpper();
                paciente.Telefono = telefono;
                paciente.Email = email;

                string hash;
                do
                {
                    Random numero = new Random();
                    int randomize = numero.Next(0, 61);
                    string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                    string get_1;
                    get_1 = aleatorio[randomize];
                    hash = get_1;
                    for (int i = 0; i < 9; i++)
                    {
                        randomize = numero.Next(0, 61);
                        get_1 = aleatorio[randomize];
                        hash += get_1;
                    }
                } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                paciente.HASH = hash;


                //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                //Se obtiene el número del contador desde la base de datos
                int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                //Contadores por número de citas en cada sucursal
                string contador = "";
                if (num == null)
                {
                    contador = "100";
                }
                else if (num < 10)
                {
                    contador = "00" + Convert.ToString(num);
                }
                else if (num >= 10 && num < 100)
                {
                    contador = "0" + Convert.ToString(num);
                }
                else
                {
                    contador = Convert.ToString(num);
                }

                //Se asigna el número de ID del doctor
                //if (Convert.ToInt32(doc) < 10)
                //{
                //    doc = "0" + doc;
                //}

                string mes = DateTime.Now.Month.ToString();
                string dia = DateTime.Now.Day.ToString();
                char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                string anio = "";

                for (int i = 2; i < year.Length; i++)
                {
                    anio += year[i];
                }

                if (Convert.ToInt32(mes) < 10)
                {
                    mes = "0" + mes;
                }

                if (Convert.ToInt32(dia) < 10)
                {
                    dia = "0" + dia;
                }

                //Se crea el número de Folio
                //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                string numFolio = dia + mes + anio + SUC + "-" + contador;
                paciente.Folio = dia + mes + anio + SUC + "-" + contador;

                if (ModelState.IsValid)
                {
                    db.Paciente.Add(paciente);
                    db.SaveChanges();
                    //return RedirectToAction("Index");
                }

                int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                Sucursales suc = db.Sucursales.Find(idSuc);

                suc.Contador = Convert.ToInt32(num);

                if (ModelState.IsValid)
                {
                    db.Entry(suc).State = EntityState.Modified;
                    db.SaveChanges();
                    //No retorna ya que sigue el proceso
                    //return RedirectToAction("Index");
                }

                var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                Cita cita = new Cita();

                cita.TipoPago = "Pago con Tarjeta";

                cita.NoOrden = IDEE;

                cita.idPaciente = idPaciente;
                cita.FechaReferencia = DateTime.Now;
                cita.Sucursal = sucursal;
                cita.Recepcionista = usuario;
                cita.EstatusPago = "Pendiente";
                cita.Folio = numFolio;
                cita.Canal = "Recepción";
                cita.FechaCita = fecha != null ? fecha : DateTime.Now;
                cita.NoOrden = link;
                cita.Referencia = Convert.ToString(card);
                cita.FechaCreacion = DateTime.Now;

                if (cantidadATInt == 0 && cantidadAPInt == 0)
                {
                    if (tipoGestor == "OTRO")
                    {
                        double calculoIVA = 0;
                        var precioSoloEpi = (ALV / 1.16);
                        double ventaTotal = 0;
                        double IVATotal = 0;
                        double ventaTotalIVA = 0;

                        calculoIVA = ALV - (ALV / 1.16);
                        precioSoloEpi = (ALV / 1.16);
                        cita.Venta = Convert.ToString(precioSoloEpi);
                        cita.IVA = Convert.ToString(calculoIVA);


                        if (cb_Seguro != null)
                        {
                            cita.ventaSeguro = "SI";
                            cita.CostoSeguro = "100";
                            cita.IvaSeguro = "16";
                            cita.TotalSeguro = "116";
                            ventaTotal = precioSoloEpi + 100;
                            cita.TotalVenta = Convert.ToString(ventaTotal);
                            IVATotal = calculoIVA + 16;
                            cita.TotalIVA = Convert.ToString(IVATotal);
                            ventaTotalIVA = ventaTotal + IVATotal;
                            cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                        }
                        else
                        {
                            cita.ventaSeguro = "NO";
                            cita.CostoSeguro = "32";
                            cita.IvaSeguro = "5.12";
                            cita.TotalSeguro = "37.12";
                            ventaTotal = precioSoloEpi + 32;
                            cita.TotalVenta = Convert.ToString(ventaTotal);
                            IVATotal = calculoIVA + 5.12;
                            cita.TotalIVA = Convert.ToString(IVATotal);
                            ventaTotalIVA = ventaTotal + IVATotal;
                            cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                        }
                    }
                }

                //Se usa el idCanal para poder hacer que en Recepción se tenga que editar el nombre si viene de gestor
                cita.idCanal = 1;

                string TIPOLIC = null;
                if (cantidadA != 0)
                {
                    TIPOLIC = "AEREO";
                }
                if (cantidadAP != 0)
                {
                    TIPOLIC = "AEREO_PISTA";
                }
                cita.TipoLicencia = TIPOLIC;

                //if (referido == "NINGUNO" || referido == "OTRO")
                //{
                //    cita.CC = "N/A";
                //}
                //else
                //{
                //    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                //    cita.CC = referidoTipo;
                //}

                var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
                cita.CC = referidoTipo.Tipo;
                cita.CanalTipo = referidoTipo.Tipo;

                cita.ReferidoPor = referidoTipo.Nombre;

                if (referidoTipo.idReferido == 7 || referidoTipo.idReferido == 8 || referidoTipo.idReferido == 9 || referidoTipo.idReferido == 10 || referidoTipo.idReferido == 12 ||
                    referidoTipo.idReferido == 13 || referidoTipo.idReferido == 20 || referidoTipo.idReferido == 26 || referidoTipo.idReferido == 27 || referidoTipo.idReferido == 29 ||
                    referidoTipo.idReferido == 36 || referidoTipo.idReferido == 52 || referidoTipo.idReferido == 53 || referidoTipo.idReferido == 54 || referidoTipo.idReferido == 55 ||
                    referidoTipo.idReferido == 56 || referidoTipo.idReferido == 59 || referidoTipo.idReferido == 130 || referidoTipo.idReferido == 136 || referidoTipo.idReferido == 142)
                {
                    cita.Cuenta = "CORPORATIVO";
                }


                if (ModelState.IsValid)
                {
                    db.Cita.Add(cita);
                    db.SaveChanges();
                    //no regresa ya que se debe ver la pantalla de Orden
                    //return RedirectToAction("Index");
                }

                //MACRO TABLA
                CondensadoPaciente condensadoPaciente = new CondensadoPaciente();

                var tablaCita = (from r in db.Cita where r.idPaciente == idPaciente select r).FirstOrDefault();
                var tablaPaciente = (from w in db.Paciente where w.idPaciente == idPaciente select w).FirstOrDefault();

                condensadoPaciente.Nombre = tablaPaciente.Nombre;
                condensadoPaciente.Telefono = tablaPaciente.Telefono;
                condensadoPaciente.Email = tablaPaciente.Email;
                condensadoPaciente.Folio = tablaPaciente.Folio;
                condensadoPaciente.idCita = tablaCita.idCita;
                condensadoPaciente.TipoPago = tablaCita.TipoPago;
                condensadoPaciente.FechaReferencia = tablaCita.FechaReferencia;
                condensadoPaciente.NoOrden = tablaCita.NoOrden;
                condensadoPaciente.EstatusPago = tablaCita.EstatusPago;
                condensadoPaciente.TipoLicencia = tablaCita.TipoLicencia;
                condensadoPaciente.NoExpediente = tablaCita.NoExpediente;
                condensadoPaciente.FechaCita = tablaCita.FechaCita;
                condensadoPaciente.Sucursal = tablaCita.Sucursal;
                condensadoPaciente.Doctor = tablaCita.Doctor;
                condensadoPaciente.Recepcionista = tablaCita.Recepcionista;
                condensadoPaciente.idPaciente = tablaCita.idPaciente;
                condensadoPaciente.TipoTramite = tablaCita.TipoTramite;
                condensadoPaciente.Referencia = tablaCita.Referencia;
                condensadoPaciente.Canal = tablaCita.Canal;
                condensadoPaciente.CC = tablaCita.CC;
                condensadoPaciente.Asistencia = tablaCita.Asistencia;
                condensadoPaciente.CancelaComentario = tablaCita.CancelaComentario;
                condensadoPaciente.Entregado = tablaCita.Entregado;
                condensadoPaciente.ReferidoPor = tablaCita.Referencia;
                condensadoPaciente.FechaCreacion = tablaCita.FechaCreacion;
                condensadoPaciente.Cuenta = tablaCita.Cuenta;
                condensadoPaciente.CanalTipo = tablaCita.CanalTipo;
                condensadoPaciente.CuentaComentario = tablaCita.CuentaComentario;
                condensadoPaciente.Conciliado = tablaCita.Conciliado;
                condensadoPaciente.FechaContable = tablaCita.FechaContable;
                condensadoPaciente.UsarSaldo = tablaCita.UsarSaldo;
                condensadoPaciente.FormaPago = tablaCita.FormaPago;
                condensadoPaciente.PrecioEpi = tablaCita.PrecioEpi;
                condensadoPaciente.ConciliarPago = tablaCita.ConciliarPago;
                condensadoPaciente.RastreoPagos = tablaCita.RastreoPagos;
                condensadoPaciente.Venta = tablaCita.Venta;
                condensadoPaciente.IVA = tablaCita.IVA;
                condensadoPaciente.CostoSeguro = tablaCita.CostoSeguro;
                condensadoPaciente.IvaSeguro = tablaCita.IvaSeguro;
                condensadoPaciente.TotalSeguro = tablaCita.TotalSeguro;
                condensadoPaciente.TotalVenta = tablaCita.TotalVenta;
                condensadoPaciente.ventaSeguro = tablaCita.ventaSeguro;
                condensadoPaciente.TotalIVA = tablaCita.TotalIVA;
                condensadoPaciente.TotalVentaIVA = tablaCita.TotalVentaIVA;

                if (ModelState.IsValid)
                {
                    db.CondensadoPaciente.Add(condensadoPaciente);
                    db.SaveChanges();
                }

            }
            else
            {
                //return View(detallesOrden);
                for (int n = 1; n <= Convert.ToInt32((cantidadN + cantidadA + cantidadAP)); n++)
                {
                    Paciente paciente = new Paciente();

                    paciente.Nombre = nombre.ToUpper() + " " + n;
                    paciente.Telefono = telefono;
                    paciente.Email = email;


                    string hash;
                    do
                    {
                        Random numero = new Random();
                        int randomize = numero.Next(0, 61);
                        string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                        string get_1;
                        get_1 = aleatorio[randomize];
                        hash = get_1;
                        for (int i = 0; i < 9; i++)
                        {
                            randomize = numero.Next(0, 61);
                            get_1 = aleatorio[randomize];
                            hash += get_1;
                        }
                    } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                    paciente.HASH = hash;

                    //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                    string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                    //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                    //Se obtiene el número del contador desde la base de datos
                    int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                    //Contadores por número de citas en cada sucursal
                    string contador = "";
                    if (num == null)
                    {
                        contador = "100";
                    }
                    else if (num < 10)
                    {
                        contador = "00" + Convert.ToString(num);
                    }
                    else if (num >= 10 && num < 100)
                    {
                        contador = "0" + Convert.ToString(num);
                    }
                    else
                    {
                        contador = Convert.ToString(num);
                    }

                    //Se asigna el número de ID del doctor
                    //if (Convert.ToInt32(doc) < 10)
                    //{
                    //    doc = "0" + doc;
                    //}

                    string mes = DateTime.Now.Month.ToString();
                    string dia = DateTime.Now.Day.ToString();
                    char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                    string anio = "";

                    for (int i = 2; i < year.Length; i++)
                    {
                        anio += year[i];
                    }

                    if (Convert.ToInt32(mes) < 10)
                    {
                        mes = "0" + mes;
                    }

                    if (Convert.ToInt32(dia) < 10)
                    {
                        dia = "0" + dia;
                    }

                    //Se crea el número de Folio
                    //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                    //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                    string numFolio = dia + mes + anio + SUC + "-" + contador;
                    paciente.Folio = dia + mes + anio + SUC + "-" + contador;

                    if (ModelState.IsValid)
                    {
                        db.Paciente.Add(paciente);
                        db.SaveChanges();
                        //return RedirectToAction("Index");
                    }

                    int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                    Sucursales suc = db.Sucursales.Find(idSuc);

                    suc.Contador = Convert.ToInt32(num);

                    if (ModelState.IsValid)
                    {
                        db.Entry(suc).State = EntityState.Modified;
                        db.SaveChanges();
                        //No retorna ya que sigue el proceso
                        //return RedirectToAction("Index");
                    }

                    var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                    Cita cita = new Cita();

                    cita.TipoPago = "Pago con Tarjeta";

                    //SEPARACION AEREOS_PISTA
                    int sumaInicialTL = cantidadN + cantidadA;

                    if (n > cantidadN)
                    {
                        cita.TipoLicencia = "AEREO";
                    }

                    if (n > sumaInicialTL)
                    {
                        cita.TipoLicencia = "AEREO_PISTA";
                    }

                    cita.NoOrden = IDEE;
                    cita.idPaciente = idPaciente;
                    cita.FechaReferencia = DateTime.Now;
                    cita.Sucursal = sucursal;
                    cita.Recepcionista = usuario;
                    cita.EstatusPago = "Pendiente";
                    cita.Folio = numFolio;
                    cita.Canal = "Recepción";
                    cita.FechaCita = fecha != null ? fecha : DateTime.Now;
                    cita.NoOrden = link;
                    cita.Referencia = Convert.ToString(card);
                    cita.FechaCreacion = DateTime.Now;

                    if (tipoGestor == "OTRO")
                    {
                        double calculoIVA = 0;
                        double precioSoloEpi = 0;
                        double ventaTotal = 0;
                        double IVATotal = 0;
                        double ventaTotalIVA = 0;
                        int sumaNA = cantidadN + cantidadA;
                        double preciosinIVA = precio / 1.16;

                        if (n <= cantidadN)
                        {
                            calculoIVA = precioNIVA - precioN;
                            precioSoloEpi = precioN;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                        }

                        if (n > cantidadN)
                        {
                            calculoIVA = precioATIVA - precioAT;
                            precioSoloEpi = precioAT;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                        }

                        if (n > sumaNA)
                        {
                            calculoIVA = precioPIVA - precioP;
                            precioSoloEpi = precioP;
                            cita.Venta = Convert.ToString(precioSoloEpi);
                            cita.IVA = Convert.ToString(calculoIVA);
                        }

                        if (n <= cantidadN)
                        {
                            if (cb_Seguro != null)
                            {
                                cita.ventaSeguro = "SI";
                                cita.CostoSeguro = "100";
                                cita.IvaSeguro = "16";
                                cita.TotalSeguro = "116";
                                ventaTotal = precioSoloEpi + 100;
                                cita.TotalVenta = Convert.ToString(ventaTotal);
                                IVATotal = calculoIVA + 16;
                                cita.TotalIVA = Convert.ToString(IVATotal);
                                ventaTotalIVA = ventaTotal + IVATotal;
                                cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                            }
                            else
                            {
                                cita.ventaSeguro = "NO";
                                cita.CostoSeguro = "32";
                                cita.IvaSeguro = "5.12";
                                cita.TotalSeguro = "37.12";
                                ventaTotal = precioSoloEpi + 32;
                                cita.TotalVenta = Convert.ToString(ventaTotal);
                                IVATotal = calculoIVA + 5.12;
                                cita.TotalIVA = Convert.ToString(IVATotal);
                                ventaTotalIVA = ventaTotal + IVATotal;
                                cita.TotalVentaIVA = Convert.ToString(ventaTotalIVA);
                            }
                        }
                    }

                    //if (referido == "NINGUNO" || referido == "OTRO")
                    //{
                    //    cita.CC = "N/A";
                    //}
                    //else
                    //{
                    //    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                    //    cita.CC = referidoTipo;
                    //}

                    var referidoTipo = (from r in db.Referido where r.idReferido == referido select r).FirstOrDefault();
                    cita.CC = referidoTipo.Tipo;
                    cita.CanalTipo = referidoTipo.Tipo;

                    cita.ReferidoPor = referidoTipo.Nombre;

                    if (referidoTipo.idReferido == 7 || referidoTipo.idReferido == 8 || referidoTipo.idReferido == 9 || referidoTipo.idReferido == 10 || referidoTipo.idReferido == 12 ||
                    referidoTipo.idReferido == 13 || referidoTipo.idReferido == 20 || referidoTipo.idReferido == 26 || referidoTipo.idReferido == 27 || referidoTipo.idReferido == 29 ||
                    referidoTipo.idReferido == 36 || referidoTipo.idReferido == 52 || referidoTipo.idReferido == 53 || referidoTipo.idReferido == 54 || referidoTipo.idReferido == 55 ||
                    referidoTipo.idReferido == 56 || referidoTipo.idReferido == 59 || referidoTipo.idReferido == 130 || referidoTipo.idReferido == 136 || referidoTipo.idReferido == 142)
                    {
                        cita.Cuenta = "CORPORATIVO";
                    }


                    if (ModelState.IsValid)
                    {
                        db.Cita.Add(cita);
                        db.SaveChanges();
                        //no regresa ya que se debe ver la pantalla de Orden
                        //return RedirectToAction("Index");
                    }

                    //MACRO TABLA
                    CondensadoPaciente condensadoPaciente = new CondensadoPaciente();

                    var tablaCita = (from r in db.Cita where r.idPaciente == idPaciente select r).FirstOrDefault();
                    var tablaPaciente = (from w in db.Paciente where w.idPaciente == idPaciente select w).FirstOrDefault();

                    condensadoPaciente.Nombre = tablaPaciente.Nombre;
                    condensadoPaciente.Telefono = tablaPaciente.Telefono;
                    condensadoPaciente.Email = tablaPaciente.Email;
                    condensadoPaciente.Folio = tablaPaciente.Folio;
                    condensadoPaciente.idCita = tablaCita.idCita;
                    condensadoPaciente.TipoPago = tablaCita.TipoPago;
                    condensadoPaciente.FechaReferencia = tablaCita.FechaReferencia;
                    condensadoPaciente.NoOrden = tablaCita.NoOrden;
                    condensadoPaciente.EstatusPago = tablaCita.EstatusPago;
                    condensadoPaciente.TipoLicencia = tablaCita.TipoLicencia;
                    condensadoPaciente.NoExpediente = tablaCita.NoExpediente;
                    condensadoPaciente.FechaCita = tablaCita.FechaCita;
                    condensadoPaciente.Sucursal = tablaCita.Sucursal;
                    condensadoPaciente.Doctor = tablaCita.Doctor;
                    condensadoPaciente.Recepcionista = tablaCita.Recepcionista;
                    condensadoPaciente.idPaciente = tablaCita.idPaciente;
                    condensadoPaciente.TipoTramite = tablaCita.TipoTramite;
                    condensadoPaciente.Referencia = tablaCita.Referencia;
                    condensadoPaciente.Canal = tablaCita.Canal;
                    condensadoPaciente.CC = tablaCita.CC;
                    condensadoPaciente.Asistencia = tablaCita.Asistencia;
                    condensadoPaciente.CancelaComentario = tablaCita.CancelaComentario;
                    condensadoPaciente.Entregado = tablaCita.Entregado;
                    condensadoPaciente.ReferidoPor = tablaCita.Referencia;
                    condensadoPaciente.FechaCreacion = tablaCita.FechaCreacion;
                    condensadoPaciente.Cuenta = tablaCita.Cuenta;
                    condensadoPaciente.CanalTipo = tablaCita.CanalTipo;
                    condensadoPaciente.CuentaComentario = tablaCita.CuentaComentario;
                    condensadoPaciente.Conciliado = tablaCita.Conciliado;
                    condensadoPaciente.FechaContable = tablaCita.FechaContable;
                    condensadoPaciente.UsarSaldo = tablaCita.UsarSaldo;
                    condensadoPaciente.FormaPago = tablaCita.FormaPago;
                    condensadoPaciente.PrecioEpi = tablaCita.PrecioEpi;
                    condensadoPaciente.ConciliarPago = tablaCita.ConciliarPago;
                    condensadoPaciente.RastreoPagos = tablaCita.RastreoPagos;
                    condensadoPaciente.Venta = tablaCita.Venta;
                    condensadoPaciente.IVA = tablaCita.IVA;
                    condensadoPaciente.CostoSeguro = tablaCita.CostoSeguro;
                    condensadoPaciente.IvaSeguro = tablaCita.IvaSeguro;
                    condensadoPaciente.TotalSeguro = tablaCita.TotalSeguro;
                    condensadoPaciente.TotalVenta = tablaCita.TotalVenta;
                    condensadoPaciente.ventaSeguro = tablaCita.ventaSeguro;
                    condensadoPaciente.TotalIVA = tablaCita.TotalIVA;
                    condensadoPaciente.TotalVentaIVA = tablaCita.TotalVentaIVA;

                    if (ModelState.IsValid)
                    {
                        db.CondensadoPaciente.Add(condensadoPaciente);
                        db.SaveChanges();
                    }

                }
            }
            return Redirect("Index");
        }

        // GET: Pacientes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Paciente paciente = db.Paciente.Find(id);
            if (paciente == null)
            {
                return HttpNotFound();
            }
            return View(paciente);
        }

        // POST: Pacientes/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "idPaciente,Nombre,Telefono,Email,Folio")] Paciente paciente)
        {
            if (ModelState.IsValid)
            {
                db.Entry(paciente).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(paciente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarNombre(int id, string nombre)
        {
            var paciente = (from i in db.Paciente where i.idPaciente == id select i).FirstOrDefault();
            var cita = (from i in db.Cita where i.idPaciente == id select i).FirstOrDefault();
            paciente.Nombre = nombre.ToUpper();
            cita.idCanal = 1;

            if (ModelState.IsValid)
            {
                db.Entry(paciente).State = EntityState.Modified;
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(paciente);
        }

        //TIPO PAGO EN EL MODAL DE ACCIONES DE RECEPCION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstatus(int? id, string pago)
        {
            string referenciaNueva = "";
            if (pago != "Referencia Scotiabank")
            {
                //var tipoPago = (from t in db.ReferenciasSB where t.ReferenciaSB == numero select t).FirstOrDefault();

                var referenciaRepetida = (from r in db.Cita where r.idPaciente == id select r.Referencia).FirstOrDefault();
                referenciaNueva = referenciaRepetida;
                var idFlag = (from i in db.Cita where i.Referencia == referenciaRepetida orderby i.idPaciente descending select i).FirstOrDefault();
                var tipoPago = (from t in db.ReferenciasSB where t.idPaciente == idFlag.idPaciente select t).FirstOrDefault();

                if (tipoPago != null)
                {
                    ReferenciasSB refe = db.ReferenciasSB.Find(tipoPago.idReferencia);
                    refe.EstatusReferencia = "LIBRE";
                    refe.idPaciente = 0;

                    if (ModelState.IsValid)
                    {
                        db.Entry(refe).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }


            }
            else
            {
                //var tipoPago = (from t in db.ReferenciasSB where t.ReferenciaSB == numero select t).FirstOrDefault();

                var referenciaRepetida = (from r in db.Cita where r.idPaciente == id select r.Referencia).FirstOrDefault();
                var idFlag = (from i in db.Cita where i.Referencia == referenciaRepetida orderby i.idPaciente descending select i).FirstOrDefault();
                referenciaNueva = (from t in db.ReferenciasSB where t.idPaciente == idFlag.idPaciente select t.ReferenciaSB).FirstOrDefault();
                var tipoPago = (from t in db.ReferenciasSB where t.idPaciente == idFlag.idPaciente select t).FirstOrDefault();

                ReferenciasSB refe = db.ReferenciasSB.Find(tipoPago.idReferencia);
                refe.EstatusReferencia = "OCUPADO";

                if (ModelState.IsValid)
                {
                    db.Entry(refe).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            var referencia = (from r in db.Cita where r.idPaciente == id select r.Referencia).FirstOrDefault();
            var consulta = from c in db.Cita where c.Referencia == referencia select c;
            if (ModelState.IsValid)
            {
                CarruselMedico cm = new CarruselMedico();

                foreach (var item in consulta)
                {
                    Cita cita = db.Cita.Find(item.idCita);

                    //MACRO TABLA
                    var tablaCita = (from r in db.Cita where r.idPaciente == item.idPaciente select r).FirstOrDefault();
                    var condensadoPaciente = (from r in db.CondensadoPaciente where r.idCita == item.idCita select r).FirstOrDefault();

                    cm.idPaciente = item.idPaciente;
                    db.CarruselMedico.Add(cm);

                    cita.EstatusPago = "Pagado";
                    cita.TipoPago = pago;
                    cita.Referencia = referenciaNueva;

                    cita.Cuenta = pago == "Pendiente de Pago" ? "CUENTAS X COBRAR" : null;

                    db.Entry(cita).State = EntityState.Modified;

                    if (condensadoPaciente != null)
                    {
                        condensadoPaciente.EstatusPago = "Pagado";
                        condensadoPaciente.TipoPago = pago;
                        condensadoPaciente.Referencia = referenciaNueva;
                        condensadoPaciente.Cuenta = pago == "Pendiente de Pago" ? "CUENTAS X COBRAR" : null;

                        db.Entry(condensadoPaciente).State = EntityState.Modified;
                    }

                }
                db.SaveChanges();
            }

            return Redirect("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Entregado(int? id)
        {
            var idCita = (from i in db.Cita where i.idPaciente == id select i.idCita).FirstOrDefault();
            Cita cita = db.Cita.Find(idCita);

            cita.Entregado = "SI";

            if (ModelState.IsValid)
            {
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();
            }

            return Redirect("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CURP_Expediente(string id, string nombre, string numero, string curp, string tel, string email, string doctor, string tipoL, string tipoT)
        {

            //var noExpedienteRepetido = (from i in db.Cita where i.NoExpediente == numero orderby i.idCita descending select i).FirstOrDefault();


            //if (noExpedienteRepetido != null && noExpedienteRepetido.CancelaComentario != "Sobreescrito")
            //{
            //    var capturaRepetida = (from i in db.Captura where i.idPaciente == noExpedienteRepetido.idPaciente orderby i.idCaptura descending select i).FirstOrDefault();

            //    if(capturaRepetida != null && capturaRepetida.EstatusCaptura != "Terminado")
            //    {
            //        TempData["idCaptura"] = capturaRepetida.idCaptura;
            //        return Redirect("Index");
            //    }
            //    else
            //    {
            //        TempData["idPaciente"] = id;
            //        TempData["Doctor"] = doctor;
            //        TempData["TipoLicencia"] = tipoL;
            //        TempData["TipoTramite"] = tipoT;

            //        return Redirect("Index");
            //    }
            //}

            int ide = Convert.ToInt32(id);

            string NOMBRE = null;
            string TELEFONO = null;
            string EMAIL = null;
            string NOEXP = null;
            string CURP = null;

            var paciente = (from p in db.Paciente where p.idPaciente == ide select p).FirstOrDefault();
            var cita = (from p in db.Cita where p.idPaciente == ide select p).FirstOrDefault();

            if (nombre == "")
            {
                NOMBRE = paciente.Nombre;
            }
            else
            {
                NOMBRE = nombre.ToUpper();
            }

            if (tel == "")
            {
                TELEFONO = paciente.Telefono;
            }
            else
            {
                TELEFONO = tel;
            }

            if (email == "")
            {
                EMAIL = paciente.Email;
            }
            else
            {
                EMAIL = email;
            }

            if (curp == "")
            {
                CURP = paciente.CURP;
            }
            else
            {
                CURP = curp.ToUpper();
            }

            if (numero == "")
            {
                NOEXP = cita.NoExpediente;
            }
            else
            {
                NOEXP = numero;
            }

            paciente.Nombre = NOMBRE.ToUpper();
            paciente.CURP = CURP;
            paciente.Email = EMAIL;
            paciente.Telefono = TELEFONO;
            cita.NoExpediente = NOEXP;

            cita.TipoLicencia = tipoL;
            cita.Doctor = doctor;
            cita.TipoTramite = tipoT;

            //CarruselMedico cm = new CarruselMedico();
            //cm.idPaciente = paciente.idPaciente;

            var capturaExistente = (from i in db.Captura where i.idPaciente == ide select i).FirstOrDefault();
            var epivirtual = (from i in db.EPI_DictamenAptitud where i.idPaciente == ide select i).FirstOrDefault();

            if (capturaExistente == null && epivirtual != null)
            {
                Captura captura = new Captura();

                captura.idPaciente = ide;
                captura.NombrePaciente = NOMBRE.ToUpper();
                captura.NoExpediente = NOEXP;
                captura.TipoTramite = tipoT;
                captura.EstatusCaptura = "No iniciado";
                captura.Doctor = doctor;
                captura.Sucursal = cita.Sucursal;
                captura.FechaExpdiente = DateTime.Now;
                captura.CarruselMedico = "SI";

                if (ModelState.IsValid)
                {
                    db.Captura.Add(captura);
                    db.SaveChanges();
                }
            }

            if (ModelState.IsValid)
            {
                //db.CarruselMedico.Add(cm);
                db.Entry(paciente).State = EntityState.Modified;
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Digitalizar(HttpPostedFileBase file, string id, string usuario, string nombre, string doctor, string numero, string tipoL,
            string tipoT, string curp, DateTime? fecha, HttpPostedFileBase ticket, HttpPostedFileBase ticket_Seguro, HttpPostedFileBase fileSeguro)
        {

            TicketSeguro ticketSeguro = new TicketSeguro();

            byte[] bytesSeguroIn = null;

            if (fileSeguro != null && fileSeguro.ContentLength > 0)
            {
                var fileName = Path.GetFileName(fileSeguro.FileName);

                byte[] bytesSeguroOut;

                using (BinaryReader br = new BinaryReader(fileSeguro.InputStream))
                {
                    bytesSeguroOut = br.ReadBytes(fileSeguro.ContentLength);

                    bytesSeguroIn = bytesSeguroOut;
                }

                ticketSeguro.FileTicketSeguro = bytesSeguroIn;
                ticketSeguro.idPaciente = Convert.ToInt32(id);

                if (ModelState.IsValid)
                {
                    db.TicketSeguro.Add(ticketSeguro);
                    db.SaveChanges();
                }
            }


            int ide = Convert.ToInt32(id);

            SCT_iCare.Expedientes exp = new SCT_iCare.Expedientes();
            var cita = (from c in db.Cita where c.idPaciente == ide select c).FirstOrDefault();
            var paciente = (from p in db.Paciente where p.idPaciente == ide select p).FirstOrDefault();

            Captura captura = new Captura();

            byte[] bytes2 = null;

            if (file != null && file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(file.InputStream))
                {
                    bytes = br.ReadBytes(file.ContentLength);
                }

                bytes2 = bytes;
            }

            byte[] bytesTicket = null;

            var consultaTicket = db.Tickets.Where(w => w.idPaciente == ide).Select(s => new { s.idPaciente, s.idTicket }).FirstOrDefault();

            if (ticket != null && ticket.ContentLength > 0)
            {
                var fileName = Path.GetFileName(ticket.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(ticket.InputStream))
                {
                    bytes = br.ReadBytes(ticket.ContentLength);
                }

                bytesTicket = bytes;

                if (consultaTicket != null)
                {
                    Tickets ticketArchivo = db.Tickets.Find(consultaTicket.idTicket);

                    ticketArchivo.FechaCarga = DateTime.Now;
                    ticketArchivo.idPaciente = Convert.ToInt32(id);
                    ticketArchivo.Ticket = bytesTicket;

                    if (ModelState.IsValid)
                    {
                        db.Entry(ticketArchivo).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                else
                {
                    Tickets ticketArchivo = new Tickets();

                    ticketArchivo.FechaCarga = DateTime.Now;
                    ticketArchivo.idPaciente = Convert.ToInt32(id);
                    ticketArchivo.Ticket = bytesTicket;

                    if (ModelState.IsValid)
                    {
                        db.Tickets.Add(ticketArchivo);
                        db.SaveChanges();
                    }
                }

            }

            string CURP = null;
            string NOMBRE = null;
            string NOEXPEDIENTE = null;
            DateTime FECHA = DateTime.Now;

            if (nombre == "")
            {
                NOMBRE = paciente.Nombre;
            }
            else
            {
                NOMBRE = nombre.ToUpper();
            }

            if (numero == "")
            {
                NOEXPEDIENTE = cita.NoExpediente;
            }
            else
            {
                NOEXPEDIENTE = numero;
            }

            if (curp == "")
            {
                CURP = paciente.CURP;
            }
            else
            {
                CURP = curp;
            }

            if (fecha == null)
            {
                FECHA = Convert.ToDateTime(cita.FechaCita);
            }

            exp.Expediente = bytes2;
            exp.Recpecionista = usuario;
            exp.idPaciente = ide;
            paciente.Nombre = NOMBRE;
            paciente.CURP = CURP.ToUpper();
            captura.NombrePaciente = NOMBRE;
            captura.NoExpediente = NOEXPEDIENTE;
            captura.TipoTramite = tipoT;
            captura.EstatusCaptura = "No iniciado";
            captura.Doctor = doctor;
            captura.Sucursal = cita.Sucursal;
            captura.idPaciente = Convert.ToInt32(id);
            captura.FechaExpdiente = FECHA;

            Cita citaModificar = new Cita();

            var findPrecio = (from q in db.Referido where q.Nombre == cita.ReferidoPor && q.Tipo == cita.CC select q).FirstOrDefault();
            var precioEncontradoCI = findPrecio.PrecioNormalconIVA;
            var precioEncontradoSI = findPrecio.PrecioNormal;
            var precioEncontradoACI = findPrecio.PrecioAereo;
            var precioEncontradoASI = findPrecio.PrecioAereosinIVA;
            var precioEncontradoAPCI = findPrecio.PrecioAereoPistaconIVA;
            var precioEncontradoAPSI = findPrecio.PrecioAereoPista;
            var precioFinal = "";

            if (cita.TipoPago == "Referencía BanBajío" || cita.TipoPago == "Transferencia vía BanBajío" || cita.TipoPago == "Credito Empresas"
            || cita.TipoPago == "Referencia BanBajío" || cita.TipoPago == "Banorte" || cita.TipoPago == "REFERENCIA OXXO" || cita.TipoPago == "Referencia OXXO"
            || cita.TipoPago == "Pago con Tarjeta" || findPrecio.Tipo == "EMPRESA" || cita.TipoPago == "Mercado Pago")
            {
                if (tipoL == "AEREO")
                {
                    if (findPrecio.PrecioAereo == null || findPrecio.PrecioAereo == "0")
                    {
                        precioFinal = "3712";
                    }

                    else
                    {
                        precioFinal = precioEncontradoACI;
                    }
                }
                else if (tipoL == "AEREO_PISTA")
                {
                    if (findPrecio.PrecioAereoPistaconIVA == null || findPrecio.PrecioAereoPistaconIVA == "0")
                    {
                        precioFinal = "4060";
                    }

                    else
                    {
                        precioFinal = precioEncontradoAPCI;
                    }
                }

                else
                {
                    if (findPrecio.PrecioNormalconIVA == null || findPrecio.PrecioNormalconIVA == "0")
                    {
                        precioFinal = "2784";
                    }

                    else
                    {
                        precioFinal = precioEncontradoCI;
                    }
                }
            }

            else
            {
                if (tipoL == "AEREO")
                {
                    if (findPrecio.PrecioAereosinIVA == null || findPrecio.PrecioAereosinIVA == "0")
                    {
                        precioFinal = "3500";
                    }

                    else
                    {
                        precioFinal = precioEncontradoASI;
                    }

                }
                else if (tipoL == "AEREO_PISTA")
                {
                    if (findPrecio.PrecioAereoPista == null || findPrecio.PrecioAereoPista == "0")
                    {
                        precioFinal = "3200";
                    }

                    else
                    {
                        precioFinal = precioEncontradoAPSI;
                    }

                }

                else
                {
                    if (findPrecio.PrecioNormal == null || findPrecio.PrecioNormal == "0")
                    {
                        precioFinal = "2400";
                    }

                    else
                    {
                        precioFinal = precioEncontradoSI;
                    }
                }
            }

            cita.TipoLicencia = tipoL;
            cita.TipoTramite = tipoT;
            cita.NoExpediente = NOEXPEDIENTE;
            cita.Doctor = doctor;
            cita.PrecioEpi = precioFinal;


            //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
            string SUC = (from S in db.Sucursales where S.Nombre == cita.Sucursal select S.SUC).FirstOrDefault();
            string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

            //Se obtiene el número del contador desde la base de datos del último registro de Folio incompleto
            int? num = (from c in db.Sucursales where c.Nombre == cita.Sucursal select c.Contador).FirstOrDefault() + 1;
            //var num = new string(cita.Folio.Reverse().Take(3).Reverse().ToArray());

            string contador = "";
            if (num == null)
            {
                contador = "100";
            }
            else if (num < 10)
            {
                contador = "00" + Convert.ToString(num);
            }
            else if (num >= 10 && num < 100)
            {
                contador = "0" + Convert.ToString(num);
            }
            else
            {
                contador = Convert.ToString(num);
            }

            string mes = DateTime.Now.Month.ToString();
            string dia = DateTime.Now.Day.ToString();
            char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
            string anio = "";

            for (int i = 2; i < year.Length; i++)
            {
                anio += year[i];
            }

            if (Convert.ToInt32(mes) < 10)
            {
                mes = "0" + mes;
            }

            if (Convert.ToInt32(dia) < 10)
            {
                dia = "0" + dia;
            }

            //Se crea el número de Folio
            //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
            //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

            string numFolio = dia + mes + anio + SUC + doc + contador;
            paciente.Folio = dia + mes + anio + SUC + doc + contador;
            cita.Folio = dia + mes + anio + SUC + doc + contador;

            int? idSuc = (from i in db.Sucursales where i.Nombre == cita.Sucursal select i.idSucursal).FirstOrDefault();

            Sucursales suc = db.Sucursales.Find(idSuc);

            suc.Contador = Convert.ToInt32(num);

            if (ModelState.IsValid)
            {
                db.Entry(suc).State = EntityState.Modified;
                db.SaveChanges();
                //No retorna ya que sigue el proceso
                //return RedirectToAction("Index");
            }

            var capturaExistente = (from i in db.Captura where i.idPaciente == ide select i).FirstOrDefault();

            if (capturaExistente != null)
            {
                if (ModelState.IsValid)
                {
                    db.Entry(captura).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            else
            {
                if (ModelState.IsValid)
                {
                    db.Captura.Add(captura);
                    db.SaveChanges();
                }
            }


            if (ModelState.IsValid)
            {
                db.Entry(paciente).State = EntityState.Modified;
                db.Entry(cita).State = EntityState.Modified;
                //db.Cita.Add(cita);
                db.Expedientes.Add(exp);
                //db.Captura.Add(captura);
                db.SaveChanges();

                //MACRO TABLA
                var condensadoPaciente = (from r in db.CondensadoPaciente where r.idPaciente == ide select r).FirstOrDefault();
                var tablaCaptura = (from r in db.Captura where r.idPaciente == ide select r).FirstOrDefault();

                condensadoPaciente.Nombre = paciente.Nombre;
                condensadoPaciente.CURP = paciente.CURP;
                condensadoPaciente.NoExpediente = tablaCaptura.NoExpediente;
                condensadoPaciente.TipoTramite = tablaCaptura.TipoTramite;
                condensadoPaciente.EstatusCaptura = tablaCaptura.EstatusCaptura;
                condensadoPaciente.Doctor = tablaCaptura.Doctor;
                condensadoPaciente.FechaExpdiente = tablaCaptura.FechaExpdiente;
                condensadoPaciente.TipoLicencia = cita.TipoLicencia;
                condensadoPaciente.TipoTramite = captura.TipoTramite;
                condensadoPaciente.PrecioEpi = cita.PrecioEpi;
                condensadoPaciente.Folio = cita.Folio;

                db.Entry(condensadoPaciente).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            //return View(exp);
            return RedirectToAction("Index");
        }

        // GET: Pacientes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Paciente paciente = db.Paciente.Find(id);
            if (paciente == null)
            {
                return HttpNotFound();
            }
            return View(paciente);
        }

        // POST: Pacientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Paciente paciente = db.Paciente.Find(id);
            db.Paciente.Remove(paciente);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string ConvertirCliente(string nombre, string email, string telefono)
        {
            var newClient = new Customer()
            {
                name = nombre,
                email = email,
                phone = telefono
            };
            string jsonClient = JsonConvert.SerializeObject(newClient, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return jsonClient;
        }

        private string ConvertirProductos1(string consultorio)
        {
            string producto = "Consulta EPI (" + consultorio + ")";
            var product = new LineItem()
            {
                name = "Consulta EPI" + consultorio,
                //unit_price = 258800,
                //quantity = 1
            };

            string jsonProductos = JsonConvert.SerializeObject(producto, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return jsonProductos;
        }

        private int ConvertirProductos2(string precio)
        {
            int producto = Convert.ToInt32(precio) * 100;

            int jsonProductos = Convert.ToInt32(JsonConvert.SerializeObject(producto, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
            return jsonProductos;
        }

        private long FechaExpira()
        {
            DateTime treintaDias = DateTime.Now.AddDays(364);
            long marcaTiempo = (Int64)(treintaDias.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            //string tiempo = marcaTiempo.ToString();
            return marcaTiempo;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarCompleto(HttpPostedFileBase file, string id, string nombre, string doctor, string numero, string tipoL, string tipoT
            , string pago, string telefono, string email, string referencia, string curp, DateTime? fecha, HttpPostedFileBase ticket)
        {
            int ide = Convert.ToInt32(id);

            Paciente paciente = db.Paciente.Find(ide);
            var cita = (from c in db.Cita where c.idPaciente == ide select c).FirstOrDefault();
            var expediente = (from c in db.Expedientes where c.idPaciente == ide select c).FirstOrDefault();
            var captura = (from c in db.Captura where c.idPaciente == ide select c).FirstOrDefault();

            string ID = null;
            string NOMBRE = null;
            string DOCTOR = null;
            string NUMERO = null;
            string TIPOL = null;
            string TIPOT = null;
            string PAGO = null;
            string TELEFONO = null;
            string EMAIL = null;
            string REFERENCIA = null;
            string CURP = null;
            DateTime FECHA = Convert.ToDateTime(fecha);

            if (id == null)
            {
                ID = paciente.idPaciente.ToString();
            }
            else
            {
                ID = id;
            }

            if (nombre == "")
            {
                NOMBRE = paciente.Nombre.ToUpper();
            }
            else
            {
                NOMBRE = nombre.ToUpper();
            }

            if (doctor == "")
            {
                DOCTOR = cita.Doctor;
            }
            else
            {
                DOCTOR = doctor;
            }

            if (numero == "")
            {
                NUMERO = cita.NoExpediente;
            }
            else
            {
                NUMERO = numero;
            }

            if (tipoL == "")
            {
                TIPOL = cita.TipoLicencia;
            }
            else
            {
                TIPOL = tipoL;
            }

            if (tipoT == "")
            {
                TIPOT = cita.TipoTramite;
            }
            else
            {
                TIPOT = tipoT;
            }

            if (pago == "" || pago == null)
            {
                PAGO = cita.TipoPago;
            }
            else
            {
                PAGO = pago;
            }

            if (telefono == "")
            {
                TELEFONO = paciente.Telefono;
            }
            else
            {
                TELEFONO = telefono;
            }

            if (email == "")
            {
                EMAIL = paciente.Email;
            }
            else
            {
                EMAIL = email;
            }

            if (referencia == "")
            {
                REFERENCIA = cita.Referencia;
            }
            else
            {
                REFERENCIA = referencia;
            }

            if (curp == "")
            {
                CURP = paciente.CURP;
            }
            else
            {
                CURP = curp.ToUpper();
            }

            if (fecha == null)
            {
                FECHA = Convert.ToDateTime(cita.FechaCita);
            }

            if (expediente != null)
            {
                byte[] bytes2 = expediente.Expediente;

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);

                    byte[] bytes;
                    using (BinaryReader br = new BinaryReader(file.InputStream))
                    {
                        bytes = br.ReadBytes(file.ContentLength);
                    }

                    bytes2 = bytes;
                    expediente.Expediente = bytes2;

                    if (ModelState.IsValid)
                    {
                        db.Entry(expediente).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            byte[] bytesTicket = null;

            var consultaTicket = db.Tickets.Where(w => w.idPaciente == ide).Select(s => new { s.idPaciente, s.idTicket }).FirstOrDefault();

            if (ticket != null && ticket.ContentLength > 0)
            {
                var fileName = Path.GetFileName(ticket.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(ticket.InputStream))
                {
                    bytes = br.ReadBytes(ticket.ContentLength);
                }

                bytesTicket = bytes;

                if (consultaTicket != null)
                {
                    Tickets ticketArchivo = db.Tickets.Find(consultaTicket.idTicket);

                    ticketArchivo.FechaCarga = DateTime.Now;
                    ticketArchivo.idPaciente = Convert.ToInt32(id);
                    ticketArchivo.Ticket = bytesTicket;

                    if (ModelState.IsValid)
                    {
                        db.Entry(ticketArchivo).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                else
                {
                    Tickets ticketArchivo = new Tickets();

                    ticketArchivo.FechaCarga = DateTime.Now;
                    ticketArchivo.idPaciente = Convert.ToInt32(id);
                    ticketArchivo.Ticket = bytesTicket;

                    if (ModelState.IsValid)
                    {
                        db.Tickets.Add(ticketArchivo);
                        db.SaveChanges();
                    }
                }

            }

            paciente.Nombre = NOMBRE;
            paciente.Telefono = TELEFONO;
            paciente.Email = EMAIL;
            paciente.CURP = CURP;

            cita.TipoPago = PAGO;
            cita.TipoLicencia = TIPOL;
            cita.NoExpediente = NUMERO;
            cita.TipoTramite = TIPOT;
            //cita.Referencia = REFERENCIA;
            cita.Doctor = DOCTOR;

            captura.TipoTramite = TIPOT;
            captura.NombrePaciente = NOMBRE;
            captura.NoExpediente = NUMERO;
            captura.Doctor = DOCTOR;
            captura.FechaExpdiente = FECHA;

            if (ModelState.IsValid)
            {
                db.Entry(paciente).State = EntityState.Modified;
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return Redirect("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubirArchivosEKG(HttpPostedFileBase EKG, HttpPostedFileBase NoAccidentes, HttpPostedFileBase Glucosilada, HttpPostedFileBase Otros, string id)
        {
            int ide = Convert.ToInt32(id);

            Paciente paciente = db.Paciente.Find(ide);
            var archivos = (from c in db.Archivos where c.idPaciente == ide select c).FirstOrDefault();

            if (EKG != null)
            {
                byte[] bytes2;
                var fileName = Path.GetFileName(EKG.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(EKG.InputStream))
                {
                    bytes = br.ReadBytes(EKG.ContentLength);
                }
                bytes2 = bytes;

                if (archivos == null)
                {
                    Archivos pdf = new Archivos();
                    pdf.ElectroCardiograma = bytes2;
                    pdf.idPaciente = ide;

                    if (ModelState.IsValid)
                    {
                        db.Archivos.Add(pdf);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Archivos pdf = db.Archivos.Find(archivos.idArchivos);
                    pdf.ElectroCardiograma = bytes2;

                    if (ModelState.IsValid)
                    {
                        db.Entry(pdf).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            if (NoAccidentes != null)
            {
                byte[] bytes2;
                var fileName = Path.GetFileName(NoAccidentes.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(NoAccidentes.InputStream))
                {
                    bytes = br.ReadBytes(NoAccidentes.ContentLength);
                }
                bytes2 = bytes;

                if (archivos == null)
                {
                    Archivos pdf = new Archivos();
                    pdf.NoAccidentes = bytes2;
                    pdf.idPaciente = ide;

                    if (ModelState.IsValid)
                    {
                        db.Archivos.Add(pdf);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Archivos pdf = db.Archivos.Find(archivos.idArchivos);
                    pdf.NoAccidentes = bytes2;

                    if (ModelState.IsValid)
                    {
                        db.Entry(pdf).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            if (Glucosilada != null)
            {
                byte[] bytes2;
                var fileName = Path.GetFileName(Glucosilada.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(Glucosilada.InputStream))
                {
                    bytes = br.ReadBytes(Glucosilada.ContentLength);
                }
                bytes2 = bytes;

                if (archivos == null)
                {
                    Archivos pdf = new Archivos();
                    pdf.HGlucosilada = bytes2;
                    pdf.idPaciente = ide;

                    if (ModelState.IsValid)
                    {
                        db.Archivos.Add(pdf);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Archivos pdf = db.Archivos.Find(archivos.idArchivos);
                    pdf.HGlucosilada = bytes2;

                    if (ModelState.IsValid)
                    {
                        db.Entry(pdf).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            if (Otros != null)
            {
                byte[] bytes2;
                var fileName = Path.GetFileName(Otros.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(Otros.InputStream))
                {
                    bytes = br.ReadBytes(Otros.ContentLength);
                }
                bytes2 = bytes;

                if (archivos == null)
                {
                    Archivos pdf = new Archivos();
                    pdf.ArchivosExtra = bytes2;
                    pdf.idPaciente = ide;

                    if (ModelState.IsValid)
                    {
                        db.Archivos.Add(pdf);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Archivos pdf = db.Archivos.Find(archivos.idArchivos);
                    pdf.ArchivosExtra = bytes2;

                    if (ModelState.IsValid)
                    {
                        db.Entry(pdf).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            return Redirect("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubirRevaloracion(HttpPostedFileBase file, int id)
        {
            int ide = Convert.ToInt32(id);

            Paciente paciente = db.Paciente.Find(ide);
            ExpedienteRevaloracion exp = new ExpedienteRevaloracion();

            byte[] bytes2 = null;

            if (file != null && file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);

                byte[] bytes;
                using (BinaryReader br = new BinaryReader(file.InputStream))
                {
                    bytes = br.ReadBytes(file.ContentLength);
                }

                bytes2 = bytes;
            }

            exp.ExpedienteCompleto = bytes2;
            exp.idPaciente = id;

            if (ModelState.IsValid)
            {
                db.ExpedienteRevaloracion.Add(exp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return Redirect("Index");
        }

        public ActionResult NoLlego(int id, string comentario, string pago)
        {
            var cita = (from c in db.Cita where c.idPaciente == id select c).FirstOrDefault();
            cita.Asistencia = "NO";
            cita.CancelaComentario = comentario;

            //Proceso para cancelar también las referencias Scotiabank que no se utilicen
            var referenciaRepetida = (from r in db.Cita where r.idPaciente == id select r.Referencia).FirstOrDefault();

            var idFlag = (from i in db.Cita where i.Referencia == referenciaRepetida orderby i.idPaciente descending select i).FirstOrDefault();
            var tipoPago = (from t in db.ReferenciasSB where t.idPaciente == idFlag.idPaciente select t).FirstOrDefault();

            if (pago != "Pago con Tarjeta" && pago != "Transferencia vía Scotiabank" && pago != "Transferencia vía Banbajío" && tipoPago != null)
            {
                if ((from r in db.Cita where r.Referencia == referenciaRepetida select r).Count() == 1)
                {
                    ReferenciasSB refe = db.ReferenciasSB.Find(tipoPago.idReferencia);
                    refe.EstatusReferencia = "LIBRE";
                    refe.idPaciente = 0;

                    if (ModelState.IsValid)
                    {
                        db.Entry(refe).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }


            //--------------------------------------------------------------------------------------

            if (ModelState.IsValid)
            {
                db.Entry(cita).State = EntityState.Modified;
                db.SaveChanges();

                var condensadoPaciente = (from c in db.CondensadoPaciente where c.idPaciente == id select c).FirstOrDefault();
                condensadoPaciente.Asistencia = cita.Asistencia;
                condensadoPaciente.CancelaComentario = cita.CancelaComentario;

                if (ModelState.IsValid)
                {
                    db.Entry(condensadoPaciente).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");

            }

            return Redirect("Index");
        }

        //CODIGO PARA EL CALCULO DEL TOTAL DE UNA CITA EN RECEPCION
        public JsonResult totalCita(string episN, string episAT, string episAP, int gestorId , string tipoPago, string seguro)
        {
            var findGestor = (from c in db.Referido where c.idReferido == gestorId select c).FirstOrDefault();

            int episNC = 0;
            int episATC = 0;
            int episAPC = 0;
            string IVA = "";
            double totalN;
            double totalAT;
            double totalAP;
            double sumaTotal;
            double sumaEpis;
            double costoSeguro = 0;
            string resultado;

            if (episN != "")
            {
                episNC = Convert.ToInt32(episN);
            }

            if (episAT != "")
            {
                episATC = Convert.ToInt32(episAT);
            }

            if (episAP != "")
            {
                episAPC = Convert.ToInt32(episAP);
            }

            sumaEpis = episNC + episATC + episAPC;

            if (tipoPago == "REFERENCIA OXXO" || tipoPago == "Pago con Tarjeta" || tipoPago == "Referencia OXXO" || tipoPago == "Transferencia vía BanBajío" 
                || tipoPago == "Credito Empresas" || tipoPago == "Referencia BanBajío" || tipoPago == "Referencía BanBajío" || tipoPago == "Banorte" || tipoPago == "Mercado Pago")
            {
                IVA = "Si";
            }

            if(IVA == "Si")
            {
                totalN = Convert.ToDouble(findGestor.PrecioNormalconIVA) * episNC;
                totalAT = Convert.ToDouble(findGestor.PrecioAereo) * episATC;
                totalAP = Convert.ToDouble(findGestor.PrecioAereoPistaconIVA) * episAPC;
            }
            else
            {
                totalN = Convert.ToDouble(findGestor.PrecioNormal) * episNC;
                totalAT = Convert.ToDouble(findGestor.PrecioAereosinIVA) * episATC;
                totalAP = Convert.ToDouble(findGestor.PrecioAereoPista) * episAPC;
            }

            if(seguro == "SI")
            {
                costoSeguro = sumaEpis * 116;
            }

            sumaTotal = totalN + totalAT + totalAP + costoSeguro;

            resultado = "$" + Convert.ToString(sumaTotal); 

            return Json(resultado);
        }

        public JsonResult Buscar(string dato)
        {
            string parametro;

            if (dato.All(char.IsDigit))
            {
                parametro = dato;

                List<Paciente> data = db.Paciente.ToList();
                JavaScriptSerializer js = new JavaScriptSerializer();
                var selected = data.Join(db.Cita, n => n.idPaciente, m => m.idPaciente, (n, m) => new { N = n, M = m })
                    //.Where(r => r.N.Nombre.Contains(parametro) || r.M.NoExpediente == parametro)
                    .Where(r => r.M.NoExpediente == parametro)
                    .Join(db.Captura, o => o.N.idPaciente, p => p.idPaciente, (o, p) => new { O = o, P = p })
                    .Select(S => new {
                        S.O.N.idPaciente,
                        S.O.N.Nombre,
                        S.O.M.TipoPago,
                        FechaCita = Convert.ToDateTime(S.O.M.FechaCita).ToString("dd-MMMM-yyyy"),
                        S.O.M.TipoLicencia,
                        S.O.M.NoExpediente,
                        S.O.M.Sucursal,
                        S.O.M.TipoTramite,
                        S.P.EstatusCaptura
                    });

                return Json(selected, JsonRequestBehavior.AllowGet);
            }
            else
            {
                parametro = dato.ToUpper();

                double porcentaje = 1;

                if (db.Paciente.Count() > 5000 && db.Paciente.Count() < 9000)
                {
                    porcentaje = 0.5;
                }
                else if (db.Paciente.Count() >= 9000 && db.Paciente.Count() < 14000)
                {
                    porcentaje = 0.6;
                }
                else if (db.Paciente.Count() >= 14000 && db.Paciente.Count() < 18000)
                {
                    porcentaje = 0.7;
                }
                else if (db.Paciente.Count() >= 18000 && db.Paciente.Count() < 22000)
                {
                    porcentaje = 0.8;
                }
                else if (db.Paciente.Count() >= 22000)
                {
                    porcentaje = 0.9;
                }

                List<Paciente> data = db.Paciente.Where(w => w.idPaciente > (db.Paciente.Count() * porcentaje)).ToList();
                JavaScriptSerializer js = new JavaScriptSerializer();
                var selected = data.Join(db.Cita, n => n.idPaciente, m => m.idPaciente, (n, m) => new { N = n, M = m })
                    //.Where(r => r.N.Nombre.Contains(parametro) || r.M.NoExpediente == parametro)
                    .Where(r => r.N.Nombre == parametro)
                    .Join(db.Captura, o => o.N.idPaciente, p => p.idPaciente, (o, p) => new { O = o, P = p })
                    .Select(S => new {
                        S.O.N.idPaciente,
                        S.O.N.Nombre,
                        S.O.M.TipoPago,
                        FechaCita = Convert.ToDateTime(S.O.M.FechaCita).ToString("dd-MMMM-yyyy"),
                        S.O.M.TipoLicencia,
                        S.O.M.NoExpediente,
                        S.O.M.Sucursal,
                        S.O.M.TipoTramite,
                        S.P.EstatusCaptura
                    });

                return Json(selected, JsonRequestBehavior.AllowGet);
            }

            //List<Paciente> data = db.Paciente.Where(w => w.idPaciente > (db.Paciente.Count() / 3)).ToList();
            //JavaScriptSerializer js = new JavaScriptSerializer();
            //var selected = data.Join(db.Cita, n => n.idPaciente, m => m.idPaciente, (n, m) => new { N = n, M = m })
            //    //.Where(r => r.N.Nombre.Contains(parametro) || r.M.NoExpediente == parametro)
            //    .Where(r => r.N.Nombre == parametro || r.M.NoExpediente == parametro)
            //    .Join(db.Captura, o => o.N.idPaciente, p => p.idPaciente, (o, p) => new { O = o, P = p })
            //    .Select(S => new {
            //        S.O.N.idPaciente,
            //        S.O.N.Nombre,
            //        S.O.M.TipoPago,
            //        FechaCita = Convert.ToDateTime(S.O.M.FechaCita).ToString("dd-MMMM-yyyy"),
            //        S.O.M.TipoLicencia,
            //        S.O.M.NoExpediente,
            //        S.O.M.Sucursal,
            //        S.O.M.TipoTramite,
            //        S.P.EstatusCaptura
            //    });

            //return Json(selected, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OrdenScotiabank(string nombre, string telefono, string email, string usuario, string sucursal, string cantidad, string cantidadAereo, string referido)
        {
            GetApiKey();

            int cantidadN;
            int cantidadA;

            if (cantidad == "")
            {
                cantidadN = 0;
            }
            else
            {
                cantidadN = Convert.ToInt32(cantidad);
            }

            if (cantidadAereo == "")
            {
                cantidadA = 0;
            }
            else
            {
                cantidadA = Convert.ToInt32(cantidadAereo);
            }

            int precio = (cantidadN * 3480) + (cantidadA * 4640);

            if (precio > 10000)
            {
                precio = 9990;
            }

            if (cantidadAereo == "" && cantidad == "")
            {
                return View("Index");
            }


            var referenciaSB = (from r in db.ReferenciasSB where r.EstatusReferencia == "LIBRE" select r.ReferenciaSB).FirstOrDefault();
            ViewBag.ReferenciaSB = referenciaSB;

            ViewBag.Metodo = "OXXO";

            if ((cantidadN + cantidadA) == 1)
            {
                Paciente paciente = new Paciente();

                paciente.Nombre = nombre.ToUpper();
                paciente.Telefono = telefono;
                paciente.Email = email;

                string hash;
                do
                {
                    Random numero = new Random();
                    int randomize = numero.Next(0, 61);
                    string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                    string get_1;
                    get_1 = aleatorio[randomize];
                    hash = get_1;
                    for (int i = 0; i < 9; i++)
                    {
                        randomize = numero.Next(0, 61);
                        get_1 = aleatorio[randomize];
                        hash += get_1;
                    }
                } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                paciente.HASH = hash;


                //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                //Se obtiene el número del contador desde la base de datos
                int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                //Contadores por número de citas en cada sucursal
                string contador = "";
                if (num == null)
                {
                    contador = "100";
                }
                else if (num < 10)
                {
                    contador = "00" + Convert.ToString(num);
                }
                else if (num >= 10 && num < 100)
                {
                    contador = "0" + Convert.ToString(num);
                }
                else
                {
                    contador = Convert.ToString(num);
                }

                //Se asigna el número de ID del doctor
                //if (Convert.ToInt32(doc) < 10)
                //{
                //    doc = "0" + doc;
                //}

                string mes = DateTime.Now.Month.ToString();
                string dia = DateTime.Now.Day.ToString();
                char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                string anio = "";

                for (int i = 2; i < year.Length; i++)
                {
                    anio += year[i];
                }

                if (Convert.ToInt32(mes) < 10)
                {
                    mes = "0" + mes;
                }

                if (Convert.ToInt32(dia) < 10)
                {
                    dia = "0" + dia;
                }

                //Se crea el número de Folio
                //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                string numFolio = dia + mes + anio + SUC + "-" + contador;
                paciente.Folio = dia + mes + anio + SUC + "-" + contador;

                if (ModelState.IsValid)
                {
                    db.Paciente.Add(paciente);
                    db.SaveChanges();
                    //return RedirectToAction("Index");
                }

                int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                Sucursales suc = db.Sucursales.Find(idSuc);

                suc.Contador = Convert.ToInt32(num);

                if (ModelState.IsValid)
                {
                    db.Entry(suc).State = EntityState.Modified;
                    db.SaveChanges();
                    //No retorna ya que sigue el proceso
                    //return RedirectToAction("Index");
                }

                var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                Cita cita = new Cita();

                cita.TipoPago = "Referencia Scotiabank";

                cita.Referencia = referenciaSB;

                cita.idPaciente = idPaciente;
                cita.FechaReferencia = DateTime.Now;
                cita.Sucursal = sucursal;
                cita.Recepcionista = usuario;
                cita.EstatusPago = "pending_payment";
                cita.Folio = numFolio;
                cita.Canal = "Recepción";
                cita.FechaCita = DateTime.Now;
                cita.ReferidoPor = referido.ToUpper();
                cita.FechaCreacion = DateTime.Now;

                //Se usa el idCanal para poder hacer que en Recepción se tenga que editar el nombre si viene de gestor
                cita.idCanal = 1;

                if (referido == "ELIZABETH")
                {
                    cita.Referencia = "E1293749";
                }
                if (referido == "PABLO")
                {
                    cita.Referencia = "PL1293750";
                }
                if (referido == "NATALY FRANCO")
                {
                    cita.Referencia = "NF1293751";
                }
                if (referido == "LUIS VALENCIA")
                {
                    cita.Referencia = "LV1293752";
                }
                if (referido == "ROBERTO SALAZAR")
                {
                    cita.Referencia = "RS1293753";
                }

                int idRefSB = Convert.ToInt32((from r in db.ReferenciasSB where r.ReferenciaSB == referenciaSB select r.idReferencia).FirstOrDefault());
                ReferenciasSB refe = db.ReferenciasSB.Find(idRefSB);
                refe.EstatusReferencia = "PENDIENTE";
                refe.idPaciente = idPaciente;

                string TIPOLIC = null;
                if (cantidadA != 0)
                {
                    TIPOLIC = "AEREO";
                }
                cita.TipoLicencia = TIPOLIC;

                if (referido == "NINGUNO" || referido == "OTRO")
                {
                    cita.CC = "N/A";
                }
                else
                {
                    var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                    cita.CC = referidoTipo;
                }

                if (ModelState.IsValid)
                {
                    db.Cita.Add(cita);
                    db.Entry(refe).State = EntityState.Modified;
                    db.SaveChanges();
                    //no regresa ya que se debe ver la pantalla de Orden
                    //return RedirectToAction("Index");
                }

                ViewBag.idPaciente = paciente.idPaciente;
                ViewBag.idCita = cita.idCita;
            }
            else
            {
                //return View(detallesOrden);
                for (int n = 1; n <= Convert.ToInt32((cantidadN + cantidadA)); n++)
                {
                    Paciente paciente = new Paciente();

                    paciente.Nombre = nombre.ToUpper() + " " + n;
                    paciente.Telefono = telefono;
                    paciente.Email = email;

                    string hash;
                    do
                    {
                        Random numero = new Random();
                        int randomize = numero.Next(0, 61);
                        string[] aleatorio = new string[62] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                        string get_1;
                        get_1 = aleatorio[randomize];
                        hash = get_1;
                        for (int i = 0; i < 9; i++)
                        {
                            randomize = numero.Next(0, 61);
                            get_1 = aleatorio[randomize];
                            hash += get_1;
                        }
                    } while ((from i in db.Paciente where i.HASH == hash select i) == null);

                    paciente.HASH = hash;


                    //Se obtienen las abreviaciónes de Sucursal y el ID del doctor
                    string SUC = (from S in db.Sucursales where S.Nombre == sucursal select S.SUC).FirstOrDefault();
                    //string doc = (from d in db.Doctores where d.Nombre == doctor select d.idDoctor).FirstOrDefault().ToString();

                    //Se obtiene el número del contador desde la base de datos
                    int? num = (from c in db.Sucursales where c.Nombre == sucursal select c.Contador).FirstOrDefault() + 1;

                    //Contadores por número de citas en cada sucursal
                    string contador = "";
                    if (num == null)
                    {
                        contador = "100";
                    }
                    else if (num < 10)
                    {
                        contador = "00" + Convert.ToString(num);
                    }
                    else if (num >= 10 && num < 100)
                    {
                        contador = "0" + Convert.ToString(num);
                    }
                    else
                    {
                        contador = Convert.ToString(num);
                    }

                    //Se asigna el número de ID del doctor
                    //if (Convert.ToInt32(doc) < 10)
                    //{
                    //    doc = "0" + doc;
                    //}

                    string mes = DateTime.Now.Month.ToString();
                    string dia = DateTime.Now.Day.ToString();
                    char[] year = (DateTime.Now.Year.ToString()).ToCharArray();
                    string anio = "";

                    for (int i = 2; i < year.Length; i++)
                    {
                        anio += year[i];
                    }

                    if (Convert.ToInt32(mes) < 10)
                    {
                        mes = "0" + mes;
                    }

                    if (Convert.ToInt32(dia) < 10)
                    {
                        dia = "0" + dia;
                    }

                    //Se crea el número de Folio
                    //string numFolio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;
                    //paciente.Folio = (DateTime.Now.Year).ToString() + mes + (DateTime.Now.Day).ToString() + SUC + "-" + contador;

                    string numFolio = dia + mes + anio + SUC + "-" + contador;
                    paciente.Folio = dia + mes + anio + SUC + "-" + contador;

                    if (ModelState.IsValid)
                    {
                        db.Paciente.Add(paciente);
                        db.SaveChanges();
                        //return RedirectToAction("Index");
                    }

                    int? idSuc = (from i in db.Sucursales where i.Nombre == sucursal select i.idSucursal).FirstOrDefault();

                    Sucursales suc = db.Sucursales.Find(idSuc);

                    suc.Contador = Convert.ToInt32(num);

                    if (ModelState.IsValid)
                    {
                        db.Entry(suc).State = EntityState.Modified;
                        db.SaveChanges();
                        //No retorna ya que sigue el proceso
                        //return RedirectToAction("Index");
                    }

                    var idPaciente = (from i in db.Paciente where i.Folio == paciente.Folio select i.idPaciente).FirstOrDefault();

                    Cita cita = new Cita();

                    cita.TipoPago = "Referencia Scotiabank";

                    cita.Referencia = referenciaSB;

                    cita.idPaciente = idPaciente;
                    cita.FechaReferencia = DateTime.Now;
                    cita.Sucursal = sucursal;
                    cita.Recepcionista = usuario;
                    cita.EstatusPago = "pending_payment";
                    cita.Folio = numFolio;
                    cita.Canal = "Recepción";
                    cita.FechaCita = DateTime.Now;
                    cita.ReferidoPor = referido.ToUpper();
                    cita.FechaCreacion = DateTime.Now;

                    if (referido == "ELIZABETH")
                    {
                        cita.Referencia = "E1293749";
                    }
                    if (referido == "PABLO")
                    {
                        cita.Referencia = "PL1293750";
                    }
                    if (referido == "NATALY FRANCO")
                    {
                        cita.Referencia = "NF1293751";
                    }
                    if (referido == "LUIS VALENCIA")
                    {
                        cita.Referencia = "LV1293752";
                    }
                    if (referido == "ROBERTO SALAZAR")
                    {
                        cita.Referencia = "RS1293753";
                    }

                    if (n > cantidadN)
                    {
                        cita.TipoLicencia = "AEREO";
                    }

                    int idRefSB = Convert.ToInt32((from r in db.ReferenciasSB where r.ReferenciaSB == referenciaSB select r.idReferencia).FirstOrDefault());
                    ReferenciasSB refe = db.ReferenciasSB.Find(idRefSB);
                    refe.EstatusReferencia = "PENDIENTE";
                    refe.idPaciente = idPaciente;

                    if (referido == "NINGUNO" || referido == "OTRO")
                    {
                        cita.CC = "N/A";
                    }
                    else
                    {
                        var referidoTipo = (from r in db.Referido where r.Nombre == referido select r.Tipo).FirstOrDefault();
                        cita.CC = referidoTipo;
                    }

                    if (ModelState.IsValid)
                    {
                        db.Entry(refe).State = EntityState.Modified;
                        db.Cita.Add(cita);
                        db.SaveChanges();
                        //no regresa ya que se debe ver la pantalla de Orden
                        //return RedirectToAction("Index");
                    }

                    ViewBag.idPaciente = paciente.idPaciente;
                    ViewBag.idCita = cita.idCita;
                }
            }

            QRCodeEncoder encoder = new QRCodeEncoder();
            Bitmap img = encoder.Encode("sdfsdf");
            System.Drawing.Image QR = (System.Drawing.Image)img;

            byte[] imageBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                QR.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                imageBytes = ms.ToArray();
            }

            ViewBag.QR = imageBytes;

            ViewBag.AEREO = Convert.ToInt32(cantidadA);
            ViewBag.AUTO = Convert.ToInt32(cantidadN);
            ViewBag.Precio = (Convert.ToInt32(cantidadN) * 2842) + (Convert.ToInt32(cantidadA) * 3480);


            return View();
        }

        public JsonResult BuscarDictamen(string dato)
        {
            string parametro;

            if (dato.All(char.IsDigit))
            {
                parametro = dato;

                List<Captura> data = db.Captura.ToList();
                JavaScriptSerializer js = new JavaScriptSerializer();
                var selected = data.Where(r => r.NoExpediente == parametro)
                    .Select(S => new {
                        idCaptura = S.idCaptura,
                        S.NombrePaciente,
                        S.TipoTramite,
                        S.NoExpediente,
                        S.Sucursal,
                        S.Doctor,
                        S.EstatusCaptura
                    }).FirstOrDefault();

                return Json(selected, JsonRequestBehavior.AllowGet);
            }
            else
            {
                parametro = dato.ToUpper();

                double porcentaje = 1;

                if (db.Paciente.Count() > 5000 && db.Paciente.Count() < 9000)
                {
                    porcentaje = 0.5;
                }
                else if (db.Paciente.Count() >= 9000 && db.Paciente.Count() < 14000)
                {
                    porcentaje = 0.6;
                }
                else if (db.Paciente.Count() >= 14000 && db.Paciente.Count() < 18000)
                {
                    porcentaje = 0.7;
                }
                else if (db.Paciente.Count() >= 18000 && db.Paciente.Count() < 22000)
                {
                    porcentaje = 0.8;
                }
                else if (db.Paciente.Count() >= 22000)
                {
                    porcentaje = 0.9;
                }

                List<Captura> data = db.Captura.Where(w => w.idPaciente > (db.Paciente.Count() * porcentaje)).ToList();
                JavaScriptSerializer js = new JavaScriptSerializer();
                var selected = data.Where(r => r.NombrePaciente == parametro)
                    .Select(S => new {
                        idCaptura = S.idCaptura,
                        S.NombrePaciente,
                        S.TipoTramite,
                        S.NoExpediente,
                        S.Sucursal,
                        S.Doctor,
                        S.EstatusCaptura
                    }).FirstOrDefault();

                return Json(selected, JsonRequestBehavior.AllowGet);
            }

            //List<Captura> data = db.Captura.ToList();
            //JavaScriptSerializer js = new JavaScriptSerializer();
            //var selected = data.Where(r => r.NombrePaciente == parametro || r.NoExpediente == parametro)
            //    .Select(S => new {
            //        idCaptura = S.idCaptura,
            //        S.NombrePaciente,
            //        S.TipoTramite,
            //        S.NoExpediente,
            //        S.Sucursal,
            //        S.Doctor,
            //        S.EstatusCaptura
            //    }).FirstOrDefault();

            //return Json(selected, JsonRequestBehavior.AllowGet);
        }

        public JsonResult buscarGestor(string nombre, int cantidad)
        {
            try
            {
                GMIEntities contexto = new GMIEntities();
                var lista = contexto.buscarGestor(nombre, cantidad);
                return Json(lista, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
