create table if not exists user_prefs (
  user_id text not null,
  key text not null,
  value text not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  primary key (user_id, key)
);

create index if not exists idx_user_prefs_key on user_prefs (key);
