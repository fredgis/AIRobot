USE [master]
GO

--CREATE EdgeJob LOGIN
CREATE LOGIN [edgejob] WITH PASSWORD=N'P@ssw0rd123!'
GO

ALTER LOGIN [edgejob] DISABLE
GO

--CREATE [airobotedgedb] DATABASE
CREATE DATABASE [airobotedgedb]
GO

USE [airobotedgedb]
GO

CREATE USER [edgejob] FOR LOGIN [edgejob] WITH DEFAULT_SCHEMA=[dbo]
GO

CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'P@ssw0rd123!';

--CREATE [dbo].[Events] TABLE
CREATE TABLE [dbo].[Events](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Timestamp] [bigint] NOT NULL,
	[DrillingTemperature] [decimal](9, 5) NULL,
	[DrillBitFriction] [decimal](9, 5) NULL,
	[DrillingSpeed] [decimal](9, 5) NULL,
	[LiquidCoolingTemperature] [decimal](9, 5) NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- CREATE [dbo].[Models] TABLE
CREATE TABLE Models (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Data] [varbinary](MAX) NULL,
	[Description] varchar(1000))
GO

--Create an external file format of the type JSON.
CREATE EXTERNAL FILE FORMAT InputFileFormat
WITH 
(  
   format_type = JSON
)
GO

--Create an external data source for Azure IoT Edge hub
CREATE EXTERNAL DATA SOURCE EdgeHubInput 
WITH 
(
    LOCATION = 'edgehub://'
)
GO

--Create the external stream object for Azure IoT Edge hub.
CREATE EXTERNAL STREAM RobotSensors
WITH 
(
    DATA_SOURCE = EdgeHubInput,
    FILE_FORMAT = InputFileFormat,
	LOCATION = N'RobotSensors',
    INPUT_OPTIONS = N'',
    OUTPUT_OPTIONS = N''
);
GO

--Create the external stream object for local SQL Edge database.
CREATE DATABASE SCOPED CREDENTIAL SQLCredential
WITH IDENTITY = 'edgejob', SECRET = 'P@ssw0rd123!'
GO

CREATE EXTERNAL DATA SOURCE LocalSQLOutput
WITH 
(
    LOCATION = 'sqlserver://tcp:.,1433',
    CREDENTIAL = SQLCredential
)
GO

CREATE EXTERNAL STREAM EventsTableOutput
WITH 
(
    DATA_SOURCE = LocalSQLOutput,
    LOCATION = N'airobotedgedb.dbo.Events',
    INPUT_OPTIONS = N'',
    OUTPUT_OPTIONS = N''
);
GO

--Create the streaming job and start it.
EXEC sys.sp_create_streaming_job @name=N'StreamingJob1',
	@statement= N'SELECT [Timestamp],
						 [drillingTemperature] AS [DrillingTemperature],
						 [drillBitFriction] AS [DrillBitFriction],
						 [drillingSpeed] AS [DrillingSpeed],
						 [liquidCoolingTemperature] AS [LiquidCoolingTemperature]
				  INTO [EventsTableOutput]
				  FROM [RobotSensors]'

exec sys.sp_start_streaming_job @name=N'StreamingJob1'
GO