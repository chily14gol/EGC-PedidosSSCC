using PedidosSSCC.Comun;
using Serikat.Entidades;
using System;

namespace AccesoDatos
{
    //Debemos marcar la clase como [DataContract] o [Serializable] porque sino podemos recibir este error:
    //El tipo 'Nombre de tipo' no se puede heredar de un tipo que no está marcado con DataContractAttribute o SerializableAttribute. Considere marcar el tipo base 'X' con DataContractAttribute o SerializableAttribute, o bien quitarlos del tipo derivado.
    //Type 'Type Name' cannot inherit from a type that is not marked with DataContractAttribute or SerializableAttribute. Consider marking the base type 'X' with DataContractAttribute or SerializableAttribute, or removing them from the derived type.
    [Serializable]
	public abstract class Entidad_Base : Serikat_Entidad_Base
	{
		public abstract override object ValorPK { get; }
		public abstract Constantes.Modulo Modulo { get; }
	}
}
