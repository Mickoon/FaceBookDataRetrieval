USE [FaceBookDataCollection]
GO

/****** Object:  Table [dbo].[FBDirectMessage]    Script Date: 16/01/2015 2:33:03 p.m. ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FBDirectMessage_PosNeg](
	[Id] [bigint] NOT NULL,
	[Sender_Id] [nvarchar](max) NULL,
	[Sender_Name] [nvarchar](max) NULL,
	[Recipient1_Id] [nvarchar](max) NULL,
	[Recipient1_Name] [nvarchar](max) NULL,
	[Recipient2_Id] [nvarchar](max) NULL,
	[Recipient2_Name] [nvarchar](max) NULL,
	[Message] [nvarchar](max) NULL,
	[Type] [nvarchar](max) NULL,
	[Application] [nvarchar](max) NULL,
	[Created_Time] [datetime] NULL,
	[Updated_Time] [datetime] NULL,
	[Likes] [bigint] NULL,
	[Comments] [bigint] NULL,
	[NegWords] [bigint] NULL,
	[PosWords] [bigint] NULL,
	[Length_of_Message] [bigint] NULL
 CONSTRAINT [PK_FBDirectMessage_PosNeg] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO


