using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Transactions;
using Serikat.DAL;
using System.Reflection;

namespace AccesoDatos
{
	public partial class FacturacionInternaDataContext : ITransaccion
	{
		private bool disposed;
		private TransactionScope _transaccion;

		public TransactionScope ComenzarTransaccion()
		{
			if (this.disposed) throw new ObjectDisposedException("FacturacionInternaDataContext");

			// Por defecto el tipo de transaccion es Serializable, lo cual por lo visto es muy vulnerable a deadlocks (no admite lectura simultanea). Cambiandolo a ReadCommited deberia ir mejor
			// Tambien por defecto el Timeout de la misma suele ser superior al timeout de los commands, lo cual no tiene mucho sentido 
			// Fuente: https://blogs.msdn.microsoft.com/dbrowne/2010/06/03/using-new-transactionscope-considered-harmful/ y https://social.msdn.microsoft.com/Forums/en-US/cdb207af-3de1-4afc-b049-7c9ded980138/transactionscope-and-dead-lock?forum=adodotnetentityframework
			TransactionOptions options = new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TransactionManager.MaximumTimeout };
			this._transaccion = new TransactionScope(TransactionScopeOption.Required, options);

			return this._transaccion;
		}

		public void AsignarTransaccion(TransactionScope transaccion)
		{
			if (this.disposed) throw new ObjectDisposedException("FacturacionInternaDataContext");
			this._transaccion = transaccion;
		}

		public void ConfirmarTransaccion()
		{
			if (this.disposed) throw new ObjectDisposedException("FacturacionInternaDataContext");
			if (this._transaccion == null) throw new InvalidOperationException();
			this._transaccion.Complete();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (this._transaccion != null)
				{
					this._transaccion.Dispose();
					this._transaccion = null;
				}
				this.disposed = true;
			}
			base.Dispose(disposing);
		}
	}
}
