USE [FaceBookDataCollection]
GO

/****** Object:  Table [dbo].[FBPosts]    Script Date: 16/01/2015 2:33:14 p.m. ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FBPosts_PosNeg](
	[Post_Id] [bigint] NOT NULL,
	[User_Name] [nvarchar](max) NULL,
	[User_Id] [nvarchar](max) NULL,
	[Category] [nvarchar](max) NULL,
	[Message] [nvarchar](max) NULL,
	[Picture] [nvarchar](max) NULL,
	[Link] [nvarchar](max) NULL,
	[Created_Time] [datetime] NULL,
	[Updated_Time] [datetime] NULL,
	[Type] [nvarchar](max) NULL,
	[Status_Type] [nvarchar](max) NULL,
	[Shares] [bigint] NULL,
	[Likes] [bigint] NULL,
	[Comments] [bigint] NULL,
	[PostByOther_Id] [nvarchar](max) NULL,
	[PostByOther_Name] [nvarchar](max) NULL,
	[Caption] [nvarchar](max) NULL,
	[Caption_Title] [nvarchar](max) NULL,
	[Cap_Description] [nvarchar](max) NULL,
	[NegWords] [bigint] NULL,
	[PosWords] [bigint] NULL,
	[Length_of_Message] [bigint] NULL
 CONSTRAINT [PK_FBPosts_PosNeg] PRIMARY KEY CLUSTERED 
(
	[Post_Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO


