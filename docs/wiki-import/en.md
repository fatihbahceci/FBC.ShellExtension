---
title: Send to Wiki.js
description: Import a markdown file written in Typora, with its images, into a Wiki.js 2 page from the Explorer context menu.
published: true
tags: fbctools, wiki.js, typora, markdown
editor: markdown
---

# Send to Wiki.js

**Send to Wiki.js…** is one of FBCTools' built-in tools. It takes a markdown file you wrote locally (in Typora, or any other editor) together with the images it links to, and puts it into a Wiki.js page through the wiki's API. You choose the page by its address; the images are uploaded into an asset folder that matches the page's path, and the links in the page are rewritten to point at them.

> The wiki must run **Wiki.js 2.5**, the version the tool was built against. Earlier 2.x versions lack parts of the API it uses; Wiki.js 3 has a different API. Neither is supported.
{.is-info}

## How it works

1. In Explorer, right-click a `.md` file and choose **FBCTools › Text files › Send to Wiki.js…**.
2. Paste the address of the page as your browser shows it, for example `https://wiki.example.com/en/internal/team/onboarding`. A page that does not exist yet is created; the next time you send the same file, its last address is filled in for you.
3. The first time for a wiki, FBCTools asks for an API key or your own token (below) and keeps it, encrypted for your Windows account. You can change or delete what is kept with *Tools › Wiki.js key or token…* in the FBCTools Editor.
4. If the page already exists, FBCTools asks before replacing its content. A page written with the visual or the HTML editor is not replaced; convert it to Markdown in the wiki first.
5. A window shows each step: the images found, the asset folder, every upload, the page created or updated, and finally the page's address.

The image links in the file can be written in any of these forms:

| In the file | What happens |
| --- | --- |
| `![alt](note.assets/image.png)` | Uploaded; the link points at the wiki |
| `![alt](note.assets/my%20image.png "Title")` | `%20` and other escapes are decoded to find the file; the title stays |
| `<img src="note.assets/image.png" style="zoom:50%;">` (Typora, after resizing) | Uploaded; only `src` changes, the other attributes stay |
| `![alt](C:\Pictures\image.png)` | Absolute paths work too |
| `![alt][logo]` with a line `[logo]: note.assets/logo.png` | Uploaded; the `[logo]:` line points at the wiki |
| `![alt](https://example.com/logo.png)`, `data:` | Left alone |
| A file that is not on disk | Listed as missing; its link stays as it is and the page is still saved |

An image linked from several places is uploaded once. Images inside fenced code blocks (```` ``` ```` or `~~~`) and code spans are not touched. A block indented by four spaces without a fence is not recognized as code; its images are uploaded like any other.

## The path rule

The images go into an asset folder whose path is the page's path **without the locale**:

| | Address |
| --- | --- |
| Page | `https://wiki.example.com/en/internal/third-party/about-markdown` |
| Image | `https://wiki.example.com/internal/third-party/about-markdown/image-20210622223543110.png` |

Every segment of the page path becomes a nested asset folder in lower case, as the wiki stores it (`internal` › `third-party` › `about-markdown`). Missing folders are created. By default the links are written from the site's root (`/internal/third-party/about-markdown/image.png`), so they keep working under any name the wiki is reached by; see `linkStyle` below for full addresses.

Wiki.js changes the names of uploaded files (lower case, `_` for spaces). FBCTools uploads every image under a plain ASCII name in that form (`Ekran Görüntüsü 1.PNG` becomes `ekran_goruntusu_1.png`), then reads the folder's list back and links to the name the wiki actually stored.

An upload replaces a file of the same name, and the wiki keeps no history of files. So a **new** page never replaces what is already in its folder: an image whose name is taken there is uploaded as `name-2.png`. Sending an **existing** page again replaces the images it uploaded before; the window says so for each one.

## In the wiki, before the first import

Someone with administrator rights does this once.

1. **Turn the API on.** *Administration › API Access*: switch **Enable API** on.
2. **Create a group for the key.** *Administration › Groups*, a new group, for example *API Import*:
   - **Permissions:** `read:pages`, `write:pages`, `read:assets`, `write:assets`.
   - **Page rules:** allow `read:pages`, `write:pages`, `read:assets` and `write:assets` for the paths you will import into, for example *Path starts with* `internal`, for **all locales**. Write the path **without a slash at the end**: the wiki checks the asset folder `internal` against its own path, and `internal/` does not cover it (it also covers `internal-old/…`; that is the price). The upload is checked against the asset's path, which has no locale, so a rule limited to one locale does not cover it.
3. **Create the key.** *Administration › API Access › New API Key*: a name, an **expiration** (30 days to 3 years; prefer short), and the group above rather than *Full Access*. Copy the key at once: the wiki shows it only this one time.
4. **Upload limit.** Wiki.js refuses files larger than 5 MB unless *Administration › Security › Uploads* allows more.

> Treat the key like a password: anyone who has it can write to the wiki with the group's rights. Revoke it in *API Access* when it is no longer needed.
{.is-warning}

## With your own account: the token in your browser

If you sign in to the wiki with your company account (Azure AD, for example), you can use your own session token instead of an API key. Then:

- nobody has to share a key;
- FBCTools works in the wiki with your own rights;
- the page history shows your name (with a key it shows the administrator);
- it works even when *API Access* is switched off in the wiki.

To get the token:

1. Open the wiki in your browser (Edge, Chrome) and sign in as usual.
2. Press **F12**. On the *Application* tab, choose the wiki's address under *Storage › Cookies*.
3. Copy the *Value* of the **`jwt`** row.
4. Paste it when FBCTools asks for a key. If a key is already kept, open *Tools › Wiki.js key or token…* in the FBCTools Editor, choose the wiki's address, paste the token and press **Check and save**.

Good to know:

- **A token stops working after a while.** With Wiki.js's default settings that is about 14 days; the browser renews its own token as you use the wiki, so a freshly copied one lasts longest. When it has run out, FBCTools says "The wiki did not accept the API key or token" and asks again: copy a fresh `jwt` from the browser. An administrator changes the period with *Token Expiration* and *Token Renewal Period* in *Administration › Security*.
- **Treat the token like a password.** It can do whatever you can do in the wiki in your browser. Whoever you give it to works in your name and with your rights.
- Signing out of the wiki in the browser does not invalidate a kept token; it stops working when the wiki account is disabled or the period runs out.

## Settings

FBCTools keeps the tool's settings in `%APPDATA%\FBCTools\wiki-import.json`. It is created on the first import; you only need to edit it for the options below.

```json
{
  "linkStyle": "path",
  "removeFirstHeading": true,
  "sites": [
    { "baseUrl": "https://wiki.example.com", "apiKey": "<encrypted by Windows, not the key itself>" }
  ],
  "targets": [
    { "file": "C:\\Docs\\onboarding.md", "url": "https://wiki.example.com/en/internal/team/onboarding" }
  ]
}
```

| Setting | Meaning | Default |
| --- | --- | --- |
| `linkStyle` | `path`: links from the site's root (`/internal/page/image.png`). `fullUrl`: with the wiki's address in front | `path` |
| `removeFirstHeading` | Wiki.js shows the page title above the page; a level-1 heading the file opens with, which is usually the same title, is left out | `true` |
| `sites` | One entry per wiki, with its API key or token encrypted for your Windows account (DPAPI). Change or delete it with *Tools › Wiki.js key or token…* in the FBCTools Editor | — |
| `targets` | The address each file went to last | — |

**Environment variable.** If `FBC_WIKI_APIKEY` is set, its value is used as the key for every wiki and nothing is stored.

**Title.** The page's title is the `title` of the file's YAML front matter, if it has one, otherwise the level-1 heading the file opens with (`# Title`, or `Title` underlined with `===`), otherwise the file name. A level-1 heading further down is part of the text. A front matter block is not sent to the wiki.

**Updating a page.** The content and the title come from the file; the page stays at its path, even when the address was typed in another case. The page's description, tags and published state stay as they are in the wiki, and so do its CSS and JavaScript (a *Full Access* key reads and keeps them; the group above cannot change them; with your own token your groups decide, and `write:styles` or `write:scripts` without `manage:pages` or `delete:pages` clears them). A **publishing schedule** (start and end dates) is cleared: the Wiki.js 2 API cannot read it, so it cannot be kept. A new page is created published, with no description and no tags.

**Where the item appears.** The default menu offers it for a single `.md` file under *Text files*. In the FBCTools Editor any command can run it: choose **Send to Wiki.js** as its *Kind*.

## After the import: checklist

- [ ] Open the page and check that every image shows. In *Assets*, the folder structure matches the page's path.
- [ ] Fill in the **description** and the **tags**; check the published state, and enter a publishing schedule again if the page had one.
- [ ] Check Typora-only syntax (see below); turn on the rendering modules you need in *Administration › Rendering*.
- [ ] If the wiki uses a static navigation menu, add the page to it.
- [ ] If the page does not show in the page tree, *Administration › Utilities › Content › Rebuild Page Tree*.
- [ ] With Git storage, check after the next sync (every 5 minutes by default, or *Force Sync*) that the page and its images are in the repository.
- [ ] If the key was made for this one job, revoke it in *API Access*.

## Typora syntax in Wiki.js 2

| Syntax | In Wiki.js 2 |
| --- | --- |
| `==highlight==` | Not rendered: shows as typed. Use `<mark>…</mark>` |
| `[TOC]` | Not supported. Wiki.js builds the table of contents from the headings by itself |
| `$…$`, `$$…$$` | KaTeX, on by default (MathJax is off) |
| ` ```mermaid ` | Rendered (an older Mermaid version: newer diagram types may fail) |
| `^superscript^`, `~subscript~` | Rendered |
| Footnotes `[^1]`, task lists, emoji | Rendered |
| YAML front matter | Not sent; its `title` becomes the page title |

## Limitations and troubleshooting

| What you see | Why | What to do |
| --- | --- | --- |
| "API Key is invalid or was revoked" | The key was revoked or deleted in the wiki | FBCTools asks for the key again; create a new one |
| "API is disabled" | The API is switched off | *Administration › API Access › Enable API* |
| "The wiki did not accept the API key or token" with an authentication message | The key or token has expired, was mistyped, or belongs to another wiki: Wiki.js then treats requests as a guest | FBCTools asks again. For a token, copy a fresh `jwt` from the browser; for a key, check it or create a new one |
| "The asset folder "…" is there, but the API key or token does not see it" | The page rule ends with a slash (`internal/`) | Write the rule without the slash (see above) |
| "… was not uploaded: You are not authorized to upload files" | The group lacks `write:assets`, or no page rule covers the path in all locales (with a token: your own groups) | Fix the group (see above) |
| "… was not uploaded" for a large image | Larger than the upload limit (5 MB by default) | Raise it in *Administration › Security › Uploads*, or make the image smaller |
| "… is not in the asset folder after its upload" | The wiki answered "ok" but did not keep the file | See the wiki's log |
| "The wiki refused: … (code 6005)" | The page path has a dot, space or backslash | Choose another path |
| "A page path cannot start with …" | `login`, `img`, `js` and similar are taken by the wiki itself; a two-letter first folder is read as a language | Choose another first folder |
| "No Wiki.js 2 API answers at …" | Wrong address, or Wiki.js 3 | Check the address |
| "No answer came from the wiki at … (HTTP 504 …)" | A server in front of the wiki (a proxy such as nginx or IIS) stopped waiting. Wiki.js saves a page first, and answers only after rendering it and adding it to the page tree, the search index and storage | When this happens while the page is written, FBCTools reads the page back from the wiki; if it was saved, it says "but the page is saved" and the import counts as done. If it happens often, raise the proxy's timeout (nginx: `proxy_read_timeout`) |
| "… sends the request on to …" | The address redirects, usually from `http://` to `https://` | Use the address it names |
| "The page at … was written with another editor" | The page uses the visual or the HTML editor | Convert it to Markdown in the wiki, or choose another address |
| The page is not in the tree | | *Administration › Utilities › Rebuild Page Tree* |

- Wiki.js 2 records a change made with an API key under the wiki's first user (id 1, the administrator created at setup): the history shows that account as the author. A change made with your own token is recorded under your name.
- Private pages are not available in Wiki.js 2; every page is created public within the wiki's own rights.
- The page's content travels in one request; the wiki accepts about 5 MB of text at most (its `bodyParserLimit`).
