-- Profile picture URL (served from /uploads/avatars/{user_id}.jpg)
alter table if exists users
  add column if not exists avatar_url text;

comment on column users.avatar_url is 'Relative URL path to user avatar image, e.g. /uploads/avatars/{id}.jpg';
