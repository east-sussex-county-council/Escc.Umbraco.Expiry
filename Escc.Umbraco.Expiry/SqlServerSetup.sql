/****** Object:  Table [dbo].[ExpiryEmails]    Script Date: 15/01/2018 11:36:59 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ExpiryEmails](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EmailAddress] [varchar](50) NULL,
	[DateAdded] [datetime2](7) NULL,
	[EmailSuccess] [bit] NULL,
	[pages] [varchar](max) NULL,
 CONSTRAINT [PK_ExpiryEmails] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO



/****** Object:  StoredProcedure [dbo].[SetExpiryLogDetails]    Script Date: 15/01/2018 11:37:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Cai Lehwald
-- Create date: 04/05/2017
-- Description:	Set Expiry Email Log details
-- =============================================
CREATE PROCEDURE [dbo].[SetExpiryLogDetails]
	-- Add the parameters for the stored procedure here
	@EmailAddress nvarchar(50),
	@DateAdded DateTime2,
	@EmailSuccess bit,
	@Pages varchar(max)

AS
BEGIN
	SET NOCOUNT ON;

    -- Insert statements for procedure here
INSERT INTO [dbo].[ExpiryEmails] ([EmailAddress],[DateAdded], [EmailSuccess], [Pages]) VALUES (@EmailAddress, @DateAdded, @EmailSuccess, @Pages)
END
