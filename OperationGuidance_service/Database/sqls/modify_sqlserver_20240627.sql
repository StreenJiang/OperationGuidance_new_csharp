-- ----------------------------
-- Table structure for screw_bit_counter
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[screw_bit_counter]') AND type IN ('U'))
	DROP TABLE [dbo].[screw_bit_counter]
GO

CREATE TABLE [dbo].[screw_bit_counter] (
  [id] int  IDENTITY(1,1) NOT NULL,
  [mission_id] int  NOT NULL,
  [bit_position] int  NOT NULL,
  [max_num] int  NOT NULL,
  [count_each_time] int  NOT NULL,
  [current_counts] int  NOT NULL,
  [clear_times] int  NOT NULL,
  [user_id] int  NOT NULL,
  [deleted] int  NOT NULL,
  [creator] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modifier] nvarchar(128) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [create_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [modify_time] nvarchar(64) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[screw_bit_counter] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Auto increment value for screw_bit_counter
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[screw_bit_counter]', RESEED, 1)
GO


-- ----------------------------
-- Primary Key structure for table screw_bit_counter
-- ----------------------------
ALTER TABLE [dbo].[screw_bit_counter] ADD CONSTRAINT [PK__user_acc__3213E83F9F404DB7_copy1] PRIMARY KEY CLUSTERED ([id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO

