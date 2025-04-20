namespace Meowtrix.PixivApiFS

open System

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
