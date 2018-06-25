CREATE TABLE [Admin].[Usuarios] (
    [iUsuario]  UNIQUEIDENTIFIER CONSTRAINT [DF_Usuarios_iUsuario] DEFAULT (newsequentialid()) NOT NULL,
    [Usuario]   NVARCHAR (50)    NULL,
    [Nombre]    NVARCHAR (50)    NULL,
    [Apellidos] NVARCHAR (50)    NULL,
    [Email]     NVARCHAR (50)    NULL,
    [Password]  NVARCHAR (500)   NULL,
	[PasswordSalt]  NVARCHAR (500)   NULL,
    [IsActive]  BIT              NULL,
    PRIMARY KEY CLUSTERED ([iUsuario] ASC)
);

