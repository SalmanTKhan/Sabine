-- Discriminator for items table: 0=body (default), 1=cart.
-- Storage uses a separate account-keyed table (storage_items).
ALTER TABLE `items` ADD `location` TINYINT(4) NOT NULL DEFAULT 0 AFTER `equipped`;
