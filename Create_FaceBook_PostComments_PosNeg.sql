USE [FaceBookDataCollection]
GO

/****** Object:  Table [dbo].[FBPostComments]    Script Date: 16/01/2015 2:33:09 p.m. ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FBPostComments_PosNeg](
	[Comment_Id] [bigint] NOT NULL,
	[Writer_ID] [nvarchar](max) NULL,
	[Writer_Name] [nvarchar](max) NULL,
	[Post_Id] [bigint] NULL,
	[Recipient_Id] [nvarchar](max) NULL,
	[Recipient_Name] [nvarchar](max) NULL,
	[Message] [nvarchar](max) NULL,
	[Message_Tags_Count] [bigint] NULL,
	[Created_Time] [datetime] NULL,
	[Like_Count] [bigint] NULL,
	[User_Likes] [bit] NULL,
	[NegWords] [bigint] NULL,
	[PosWords] [bigint] NULL,
	[Length_of_Message] [bigint] NULL
 CONSTRAINT [PK_FBPostComments_PosNeg] PRIMARY KEY CLUSTERED 
(
	[Comment_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO


