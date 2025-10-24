using PedidosSSCC.Comun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public class DAL_Tareas_Empresas : DAL_Base<Tareas_Empresas>
	{
		public DAL_Tareas_Empresas() : base() { }
		public DAL_Tareas_Empresas(TransactionScope transaccion) : base(transaccion) {}

		protected override System.Data.Linq.Table<Tareas_Empresas> Tabla
		{
			get { return bd.Tareas_Empresas; }
		}

		public override string ComboText { get { return "ComboText"; } }

		public override string ComboValue { get { return "ValorPK"; } }

        public bool SoloAprobadas = false;
        public bool ExcluirPermisosAprobador = false;
        public int? FiltrarAnyo = null;

        public override bool ConsultarMedianteClaveExterna
        {
            get { return true; }
        }

        public override List<Tareas_Empresas> L(bool sinFiltrar = false, Expression<Func<Tareas_Empresas, bool>> preFiltro = null)
        {
            if (SoloAprobadas)
            {
                Expression<Func<Tareas_Empresas, bool>> prefiltroAprobadas = p => p.TEM_Vigente && p.TEM_ESO_Id.Equals((int)Constantes.EstadosSolicitud.Aprobado);
                if (preFiltro == null)
                    preFiltro = prefiltroAprobadas;
                else preFiltro = preFiltro.AndAlso(prefiltroAprobadas, "p");
            }

            if (FiltrarAnyo.HasValue)
            {
                Expression<Func<Tareas_Empresas, bool>> prefiltroAnyo = p => p.TEM_Anyo.Equals(FiltrarAnyo.Value);
                if (preFiltro == null)
                    preFiltro = prefiltroAnyo;
                else preFiltro = preFiltro.AndAlso(prefiltroAnyo, "p");
            }

            List<Tareas_Empresas> retorno = (from reg in (preFiltro != null ? bd.Tareas_Empresas.Where(preFiltro) : bd.Tareas_Empresas) select reg).ToList();
            
            if (!sinFiltrar)
            {
                retorno = retorno.Where(c => c.PuedeAccederRegistro).ToList();
            }

            return retorno;
        }

        public override Tareas_Empresas L_PrimaryKey(object valorPK, bool sinFiltrar = false)
        {
            if (valorPK != null && !string.IsNullOrEmpty(valorPK.ToString()))
            {
                // Evitar llamar dos veces a ToString()
                string valorPkStr = valorPK.ToString();
                string[] arrPK = valorPkStr.Split(Constantes.SeparadorPK);

                // Verifica que el arreglo tenga exactamente 3 elementos
                if (arrPK.Length != 3)
                    return null;

                // Conversión segura de los valores
                if (!int.TryParse(arrPK[0], out int tarId) ||
                    !int.TryParse(arrPK[1], out int empId) ||
                    !int.TryParse(arrPK[2], out int anyo))
                {
                    return null;
                }

                // Utiliza el contexto existente o crea uno nuevo
                var context = bd ?? new FacturacionInternaDataContext();

                // Realiza la consulta en línea
                Tareas_Empresas retorno = context.Tareas_Empresas
                    .FirstOrDefault(reg =>
                        reg.TEM_TAR_Id == tarId &&
                        reg.TEM_EMP_Id == empId &&
                        reg.TEM_Anyo == anyo
                    );

                return retorno;
            }

            return null;
        }

        public override List<Tareas_Empresas> L_ClaveExterna(object valorFK, Expression<Func<Tareas_Empresas, bool>> preFiltro = null)
        {
            if (SoloAprobadas)
            {
                Expression<Func<Tareas_Empresas, bool>> prefiltroAprobadas = p => p.TEM_Vigente && p.TEM_ESO_Id.Equals((int)Constantes.EstadosSolicitud.Aprobado);
                if (preFiltro == null)
                    preFiltro = prefiltroAprobadas;
                else preFiltro = preFiltro.AndAlso(prefiltroAprobadas, "p");
            }
            if (FiltrarAnyo.HasValue)
            {
                Expression<Func<Tareas_Empresas, bool>> prefiltroAnyo = p => p.TEM_Anyo.Equals(FiltrarAnyo.Value);
                if (preFiltro == null)
                    preFiltro = prefiltroAnyo;
                else preFiltro = preFiltro.AndAlso(prefiltroAnyo, "p");
            }
            return (from reg in (preFiltro != null ? bd.Tareas_Empresas.Where(preFiltro) : bd.Tareas_Empresas) where reg.TEM_TAR_Id.Equals(valorFK) orderby reg.TEM_Anyo descending, reg.Empresas.EMP_Nombre select reg).ToList();
        }

        public decimal PresupuestoConsumido(int pintTAR_Id, int pintEMP_Id, int pintAnyo)
        {
            List<Tareas_Empresas_LineasEsfuerzo> lstConsumidas = (from tem in bd.Tareas_Empresas
                                         join tle in bd.Tareas_Empresas_LineasEsfuerzo on new { TAR_Id = tem.TEM_TAR_Id, EMP_Id = tem.TEM_EMP_Id, Anyo = tem.TEM_Anyo } equals new { TAR_Id = tle.TLE_TAR_Id, EMP_Id = tle.TLE_EMP_Id, Anyo = tle.TLE_Anyo } 
                                         join fle in bd.Facturas_Tareas_LineasEsfuerzo on tle.TLE_Id equals fle.FLE_TLE_Id
                                         join fac in bd.Facturas on fle.FLE_FAC_Id equals fac.FAC_Id
                                         where tem.TEM_TAR_Id.Equals(pintTAR_Id) && tem.TEM_EMP_Id.Equals(pintEMP_Id) && tem.TEM_Anyo.Equals(pintAnyo) && fac.FAC_ESO_Id.Equals((int)Constantes.EstadosSolicitud.Aprobado)
                                         select tle).ToList();

            if (lstConsumidas.Count > 0)
                return lstConsumidas.Sum(r => r.ImporteTotal);
            else 
                return 0;
        }

        protected override bool Guardar(Tareas_Empresas objTareaEmpresa, int idPersonaModificacion)
        {
            bool retorno = false;

            Tareas_Empresas item = L_PrimaryKey(objTareaEmpresa.ValorPK);

            if (item == null)
            {
                item = new Tareas_Empresas
                {
                    TEM_TAR_Id = objTareaEmpresa.TEM_TAR_Id,
                    TEM_EMP_Id = objTareaEmpresa.TEM_EMP_Id,
                    TEM_Anyo = objTareaEmpresa.TEM_Anyo,
                    FechaAlta = DateTime.Now
                };

                bd.Tareas_Empresas.InsertOnSubmit(item);
            }

            item.TEM_Elementos = objTareaEmpresa.TEM_Elementos;

            DAL_Tareas dalTareas = new DAL_Tareas();
            Tareas objTarea = dalTareas.L_PrimaryKey(item.TEM_TAR_Id);
            if (objTarea.TAR_TTA_Id == (int)Constantes.TipoTarea.PorHoras || objTarea.TAR_TTA_Id == (int)Constantes.TipoTarea.PorUnidades)
            {
                item.TEM_Presupuesto = item.TEM_Elementos * (objTarea != null ? objTarea.TAR_ImporteUnitario.Value : 1);
            }
            else
            {
                item.TEM_Presupuesto = objTareaEmpresa.TEM_Presupuesto;
            }

            item.TEM_Vigente = objTareaEmpresa.TEM_Vigente;
            item.TEM_ESO_Id = objTareaEmpresa.TEM_ESO_Id;
            item.TEM_PER_Id_Aprobador = objTareaEmpresa.TEM_PER_Id_Aprobador;
            item.TEM_FechaAprobacion = objTareaEmpresa.TEM_FechaAprobacion;
            item.TEM_ComentarioAprobacion = objTareaEmpresa.TEM_ComentarioAprobacion;

            item.FechaModificacion = DateTime.Now;
            item.PER_Id_Modificacion = idPersonaModificacion;

            bd.SubmitChanges();

			retorno = true;

			return retorno;
		}

        public bool Eliminar(int idTarea, int idEmpresa, int anio)
        {
            bool retorno = false;

            // Comprobar si existen conceptos asociadas
            bool tieneLineas = bd.Tareas_Empresas_LineasEsfuerzo.Any(tle =>
                tle.TLE_TAR_Id == idTarea &&
                tle.TLE_EMP_Id == idEmpresa &&
                tle.TLE_Anyo == anio
            );

            if (!tieneLineas)
            {
                // Buscar el registro a eliminar en Tareas_Empresas
                var tareaEmpresa = bd.Tareas_Empresas.SingleOrDefault(te =>
                    te.TEM_TAR_Id == idTarea &&
                    te.TEM_EMP_Id == idEmpresa &&
                    te.TEM_Anyo == anio 
                );

                if (tareaEmpresa != null)
                {
                    bd.Tareas_Empresas.DeleteOnSubmit(tareaEmpresa);
                    bd.SubmitChanges();
                    retorno = true;
                }
            }

            return retorno;
        }


        public List<int> ObtenerAnyos()
        {
            return (from reg in Tabla orderby reg.TEM_Anyo select reg.TEM_Anyo).Distinct().OrderByDescending(p => p).ToList();
        }
    }
}
