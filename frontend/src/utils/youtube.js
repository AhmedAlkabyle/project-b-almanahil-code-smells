// ─── WHAT THIS HELPER IS FOR (in plain words) ───
// Teachers paste a normal YouTube link. These little tools read the video's id out
// of that link so the app can show a thumbnail picture and a clean "watch" address.
// Small YouTube helpers shared by the Learning Materials views. Stage 1 stores a raw
// YouTube link; these extract the video id so we can show a thumbnail + a clean watch URL.

/** Extract an 11-char YouTube video id from the common URL shapes; null if none found. */
export function youtubeId(url) {
  if (!url) return null
  const s = String(url).trim()
  const patterns = [
    /(?:youtube\.com\/watch\?(?:.*&)?v=)([\w-]{11})/,
    /(?:youtu\.be\/)([\w-]{11})/,
    /(?:youtube\.com\/embed\/)([\w-]{11})/,
    /(?:youtube\.com\/shorts\/)([\w-]{11})/,
    /(?:youtube\.com\/live\/)([\w-]{11})/
  ]
  for (const re of patterns) {
    const m = s.match(re)
    if (m) return m[1]
  }
  return null
}

/** A thumbnail image URL for a video id (or null). */
export function youtubeThumb(id) {
  return id ? `https://img.youtube.com/vi/${id}/hqdefault.jpg` : null
}

/** A clean watch URL for a video id, falling back to the original link. */
export function youtubeWatchUrl(id, fallback = '#') {
  return id ? `https://www.youtube.com/watch?v=${id}` : fallback
}
