using Serikat.DAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace AccesoDatos
{
    public abstract class DAL_Base<T> : Serikat_DAL_Base<T, FacturacionInternaDataContext> where T: Entidad_Base, new()
	{
		public DAL_Base(bool escribirQueryLog = false)
			: base(new FacturacionInternaDataContext(ConfigurationManager.ConnectionStrings["AccesoDatos.Properties.Settings.FacturacionInternaConnectionString"].ConnectionString), escribirQueryLog)
		{
		}

		public DAL_Base(FacturacionInternaDataContext bd, bool escribirQueryLog = false)
			: base(bd, escribirQueryLog)
		{
		}

		public DAL_Base(TransactionScope transaccion, bool escribirQueryLog = false, FacturacionInternaDataContext bd = null)
			: base((bd != null)? bd : new FacturacionInternaDataContext(ConfigurationManager.ConnectionStrings["AccesoDatos.Properties.Settings.FacturacionInternaConnectionString"].ConnectionString), transaccion, escribirQueryLog)
		{
			
		}

		public override void ReinicializarContexto()
		{
			this.bd = new FacturacionInternaDataContext();
		}

		protected abstract override Table<T> Tabla { get; }

		protected abstract override bool Guardar(T entidad, int idPersonaModificacion);

		/// <summary>
		/// Empleado para meter la logica de PuedeAccederRegistro como condiciones en la WHERE y evitar el acceso a recorrer los datos a posteriori y filtrar aquellos a los que no se tiene acceso
		/// Emplear en la medida de lo posible, especialmente en metodos de buscadores, para evitar subconsultas a base de datos
		/// </summary>
		/// <returns></returns>
		public virtual Expression<Func<T, bool>> ObtenerPuedeAccederRegistro()
		{
			return null;
		}

		public override List<T> L(bool sinFiltrar = false, Expression<Func<T, bool>> preFiltro = null)
		{
			if (sinFiltrar) return base.L(sinFiltrar, preFiltro);

			Expression<Func<T, bool>> seguridadHorizontalDefinida = ObtenerPuedeAccederRegistro();
			if (seguridadHorizontalDefinida == null)
			{
				return base.L(sinFiltrar, preFiltro);
			}
			else
			{
				if (preFiltro == null)
				{
					return base.L(true, seguridadHorizontalDefinida);
				}else
				{
					Expression<Func<T, bool>> filtrosAplicados = seguridadHorizontalDefinida.AndAlso(preFiltro, "p"); // TODO Revisar, mejorable
					return base.L(true, filtrosAplicados);
				}
			}
		}
	}
}
