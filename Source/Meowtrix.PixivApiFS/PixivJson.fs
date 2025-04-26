namespace Meowtrix.PixivApiFS

open System
open System.Text.Json.Nodes

type AuthUser =
    { profile_image_urls:
        {| px_16x16: Uri;
           px_50x50: Uri;
           px_170x170: Uri |};
      id: string;
      name: string;
      account: string;
      mail_address: string;
      is_premium: bool;
      x_restrict: int;
      is_mail_authorized: bool }

type UserSummary =
    { id: int;
      name: string;
      account: string;
      profile_image_urls: {| medium: Uri |};
      is_followed: bool;
      comment: string voption }

type UserDetail =
    { user: UserSummary;
      profile: JsonObject;
      profile_publicity: JsonObject;
      workspace: JsonObject; }

type PreviewImageUrls =
    { square_medium: Uri;
      medium: Uri;
      large: Uri }
      
type OriginalImageUrls =
    { square_medium: Uri;
      medium: Uri;
      large: Uri;
      original: Uri }

type IllustDetail =
    { id: int;
      title: string;
      ``type`` : string;
      image_urls: PreviewImageUrls;
      caption: string;
      restrict: int;
      user: UserSummary;
      tags: {| name: string; translated_name: string voption |} list;
      tools: string list;
      create_date: DateTimeOffset;
      page_count: int;
      width: int;
      height: int;
      sanity_level: int;
      x_restrict: int;
      series: {| id: int; title: string |} voption;
      meta_single_page: {| original_image_url: Uri voption |};
      meta_pages: {| image_urls: OriginalImageUrls |} list;
      total_view: int;
      total_bookmarks: int;
      is_bookmarked: bool;
      visible: bool;
      is_muted: bool;
      total_comments: int }

type IllustDetailResponse =
    { illust: IllustDetail }

type IllustList =
    { illusts: IllustDetail list;
      next_url: Uri voption }

type NovelDetail =
    { id: int;
      title: string;
      caption: string;
      restrict: int;
      x_restrict: int;
      is_original: bool;
      image_urls: PreviewImageUrls;
      create_date: DateTimeOffset;
      tags: {| name: string; translated_name: string voption; added_by_uploaded_user: bool |} list;
      page_count: int;
      text_length: int;
      user: UserSummary;
      series: {| id: int; title: string |} voption;
      is_bookmarked: bool;
      total_bookmarks: int;
      total_view: int;
      visible: bool;
      total_comments: int;
      is_muted: bool;
      is_my_pixiv_only: bool;
      is_x_restricted: bool;
      novel_ai_type: int }

type NovelDetailResponse =
    { novel: NovelDetail }
    
type NovelList =
    { novels: NovelDetail list;
      next_url: Uri voption }

type NovelTextResponse =
    { novel_marker: obj;
      novel_text: string }

type NovelSeries =
    { novel_series_detail:
        {| id: int;
           title: string;
           caption: string;
           is_original: string;
           is_concluded: string;
           content_count: int;
           total_character_count: int;
           user: UserSummary;
           display_text: string;
           novel_al_type: int;
           watch_list_added: int; |}
      novel_series_first_novel: NovelDetail;
      novel_series_last_novel: NovelDetail;
      novels: NovelDetail list;
      next_url: Uri voption }
