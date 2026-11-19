CREATE TABLE `named_levels` (
	`id` text PRIMARY KEY NOT NULL,
	`name_key` text NOT NULL,
	`name` text NOT NULL,
	`version` integer NOT NULL,
	`payload` text NOT NULL,
	`saved_at` text NOT NULL,
	`author_id` text NOT NULL,
	`author_email` text NOT NULL,
	`author_name` text NOT NULL,
	`operation_id` text NOT NULL
);
--> statement-breakpoint
CREATE UNIQUE INDEX `named_levels_name_key_unique` ON `named_levels` (`name_key`);--> statement-breakpoint
CREATE TABLE `named_revisions` (
	`level_id` text NOT NULL,
	`version` integer NOT NULL,
	`payload` text NOT NULL,
	`name` text NOT NULL,
	`saved_at` text NOT NULL,
	`author_id` text NOT NULL,
	`author_email` text NOT NULL,
	`author_name` text NOT NULL,
	`note` text NOT NULL,
	`operation_id` text NOT NULL,
	PRIMARY KEY(`level_id`, `version`)
);
--> statement-breakpoint
CREATE UNIQUE INDEX `named_revision_operation` ON `named_revisions` (`operation_id`);--> statement-breakpoint
CREATE TABLE `series_imports` (
	`key` text PRIMARY KEY NOT NULL
);
