-- ----------------------------
-- Table structure for mat_code_map_whyc
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[mat_code_map_whyc]') AND type IN ('U'))
	DROP TABLE [dbo].[mat_code_map_whyc]
GO

CREATE TABLE [dbo].[mat_code_map_whyc] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [mat_code] nvarchar(256) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [parameter_set] int  NOT NULL,
  [macs_id] int  NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[mat_code_map_whyc] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Auto increment value for mat_code_map_whyc
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[mat_code_map_whyc]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table mat_code_map_whyc
-- ----------------------------
ALTER TABLE [dbo].[mat_code_map_whyc] ADD CONSTRAINT [PK__user_acc__3213E83F9F404DB7_copy1] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO

