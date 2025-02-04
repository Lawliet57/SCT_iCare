﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SCT_iCare.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeUser : AuthorizeAttribute
    {
        private Usuarios oUser;
        private GMIEntities db = new GMIEntities();
        private int idOperacion;

        public AuthorizeUser(int idOperacion = 0)
        {
            this.idOperacion = idOperacion;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            String nombreOperacion = "";
            String nombreModulo = "";
            try
            {
                oUser = (Usuarios)HttpContext.Current.Session["User"];
                var lstMisOperaciones = from m in db.rol_operacion where m.idRol == oUser.idRol && m.idOperacion == idOperacion select m;

                if (lstMisOperaciones.ToList().Count() == 0)
                {
                    var oOperacion = db.operaciones.Find(idOperacion);
                    int? idModulo = oOperacion.idModulo;
                    nombreOperacion = getNombreDeOperacion(idOperacion);
                    nombreModulo = getNombreDelModulo(idModulo);
                    filterContext.Result = new RedirectResult("~/Error/UnauthorizedOperation?operacion=" + nombreOperacion);
                }
            }
            catch
            {
                filterContext.Result = new RedirectResult("~/Error/UnauthorizedOperation?operacion=" + nombreOperacion);
            }


        }

        public string getNombreDeOperacion(int idOperacion)
        {
            var ope = from op in db.operaciones where op.id == idOperacion select op.nombre;

            String nombreOperacion;
            try
            {
                nombreOperacion = ope.First();
            }
            catch
            {
                nombreOperacion = "";
            }

            return nombreOperacion;
        }

        public string getNombreDelModulo(int? idModulo)
        {
            var modulo = from m in db.modulo where m.id == idModulo select m.nombre;

            String nombreModulo;
            try
            {
                nombreModulo = modulo.First();
            }
            catch
            {
                nombreModulo = "";
            }

            return nombreModulo;
        }

    }
}