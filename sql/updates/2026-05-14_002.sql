-- Guild treasury + persisted emblem image. Promotes the in-memory
-- Guild.Zeny / Guild.EmblemData scratch fields to DB-backed columns
-- so neither survives only in memory across a restart.
--
-- - `zeny`        BIGINT, default 0. Player deposits/withdrawals go
--                 through CZ_GUILD_ZENY; the column matches the player
--                 zeny width.
-- - `emblemData`  MEDIUMBLOB, nullable. Holds the raw BMP bytes the
--                 master uploads via CZ_REGISTER_GUILD_EMBLEM_IMG.
--                 MEDIUMBLOB caps at ~16MB; rA emblems are <2KB so
--                 there's plenty of headroom for future custom art.

ALTER TABLE `guilds`
	ADD COLUMN `zeny` BIGINT NOT NULL DEFAULT 0 AFTER `notice`,
	ADD COLUMN `emblemData` MEDIUMBLOB NULL AFTER `emblemId`;
