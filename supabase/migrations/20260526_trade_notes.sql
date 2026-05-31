-- Personal notes on paper trades (why bought, planned exit, etc.)
alter table if exists trades
  add column if not exists note text;

comment on column trades.note is 'Optional user note for this trade execution';
