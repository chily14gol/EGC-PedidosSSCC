ALTER TABLE Tareas_Empresas_LineasEsfuerzo 
	ADD TLE_Inversion BIT NOT NULL DEFAULT 0;
GO

ALTER TABLE Empresas
	ADD EMP_EmpresaFacturar_Id int NULL;
GO