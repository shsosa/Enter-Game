using UnityEngine;

namespace FeelinSlimy
{
    public static class ProceduralVisuals
    {
        private static Sprite _calmSlimeSprite;
        private static Sprite _angrySlimeSprite;
        private static Sprite _angerCreatureSprite;
        private static Sprite _largeEnemySprite;
        private static Sprite _platformSprite;
        private static Sprite _exitPortalSprite;
        private static Sprite _circleSprite;
        private static Sprite _ringSprite;
        private static Sprite _squareSprite;
        private static Sprite _residuePatchSprite;
        private static Sprite _slimeDropletSprite;

        public static Sprite GetResiduePatchSprite()
        {
            if (_residuePatchSprite != null) return _residuePatchSprite;
            int width = 48;
            int height = 24;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - width * 0.5f) / (width * 0.46f);
                    float ny = (y - height * 0.35f) / (height * 0.52f);
                    if (ny < 0f) ny *= 1.8f;

                    float wave = Mathf.Sin(nx * 5.2f) * 0.1f + Mathf.Cos(nx * 9.5f) * 0.06f;
                    float dist = nx * nx + (ny - wave) * (ny - wave);

                    if (dist > 1.0f || y < 2)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1f - dist) * 2.4f);
                    bool highlight = ny > 0.15f && ny < 0.55f && Mathf.Abs(nx + 0.18f) < 0.35f;
                    Color c = highlight
                        ? new Color(1f, 1f, 1f, alpha * 0.95f)
                        : new Color(0.88f, 0.96f, 1f, alpha * 0.85f);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            _residuePatchSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.3f), 32f);
            _residuePatchSprite.name = "ProceduralSlimeResidue";
            return _residuePatchSprite;
        }

        public static Sprite GetSlimeDropletSprite()
        {
            if (_slimeDropletSprite != null) return _slimeDropletSprite;
            int size = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.42f);
                    float ny = (y - size * 0.5f) / (size * 0.42f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool shine = nx < -0.15f && ny > 0.15f && r < 0.65f;
                    float alpha = Mathf.Clamp01((1f - r) * 2.5f);
                    Color c = shine ? new Color(1f, 1f, 1f, alpha) : new Color(0.9f, 0.95f, 1f, alpha);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            _slimeDropletSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            _slimeDropletSprite.name = "ProceduralSlimeDroplet";
            return _slimeDropletSprite;
        }

        public static Sprite GetSquareSprite()
        {
            if (_squareSprite != null) return _squareSprite;
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] cols = new Color[16 * 16];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            _squareSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            _squareSprite.name = "ProceduralSquare";
            return _squareSprite;
        }

        public static Sprite GetPlatformSprite()
        {
            if (_platformSprite != null) return _platformSprite;
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                    bool topTrim = y >= size - 5 && !border;
                    if (border)
                        tex.SetPixel(x, y, new Color(0.55f, 0.6f, 0.68f, 1f));
                    else if (topTrim)
                        tex.SetPixel(x, y, new Color(0.92f, 0.96f, 1f, 1f));
                    else
                        tex.SetPixel(x, y, new Color(0.78f, 0.82f, 0.88f, 1f));
                }
            }
            tex.Apply();
            _platformSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _platformSprite.name = "ProceduralPlatform";
            return _platformSprite;
        }

        public static Sprite GetSlimeSprite(bool isAngry)
        {
            if (isAngry && _angrySlimeSprite != null) return _angrySlimeSprite;
            if (!isAngry && _calmSlimeSprite != null) return _calmSlimeSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.44f);
                    float ny = (y - size * 0.38f) / (size * 0.44f);
                    if (ny < 0f) ny *= 1.45f;

                    float dist = nx * nx + ny * ny;
                    if (dist > 1.0f || y < 4)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool isOutline = dist > 0.82f || y < 7;
                    Color bodyCol = isOutline ? new Color(0.7f, 0.75f, 0.8f, 1f) : Color.white;

                    float hx = nx + 0.32f;
                    float hy = ny - 0.35f;
                    if (!isOutline && (hx * hx + hy * hy) < 0.08f)
                    {
                        bodyCol = new Color(1f, 1f, 1f, 1f);
                    }

                    float leftEyeX = (x - 22f) / 3.8f;
                    float rightEyeX = (x - 42f) / 3.8f;
                    float eyeY = (y - 28f) / 4.8f;
                    bool inLeftEye = (leftEyeX * leftEyeX + eyeY * eyeY) <= 1f;
                    bool inRightEye = (rightEyeX * rightEyeX + eyeY * eyeY) <= 1f;

                    if (isAngry)
                    {
                        float leftBrowY = 34f - (x - 16f) * 0.45f;
                        float rightBrowY = 34f - (48f - x) * 0.45f;
                        bool inLeftBrow = x >= 16 && x <= 28 && Mathf.Abs(y - leftBrowY) <= 2.2f;
                        bool inRightBrow = x >= 36 && x <= 48 && Mathf.Abs(y - rightBrowY) <= 2.2f;

                        if (inLeftBrow || inRightBrow)
                        {
                            bodyCol = new Color(0.15f, 0.05f, 0.05f, 1f);
                        }
                        else if ((inLeftEye || inRightEye) && y < leftBrowY && y < rightBrowY)
                        {
                            bodyCol = new Color(1f, 0.95f, 0.2f, 1f);
                        }
                    }
                    else
                    {
                        if (inLeftEye || inRightEye)
                        {
                            bodyCol = new Color(0.12f, 0.16f, 0.22f, 1f);
                            if (y >= 29 && ((x >= 20 && x <= 22) || (x >= 40 && x <= 42)))
                            {
                                bodyCol = Color.white;
                            }
                        }
                    }

                    tex.SetPixel(x, y, bodyCol);
                }
            }

            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = isAngry ? "ProceduralSlime_Angry" : "ProceduralSlime_Calm";

            if (isAngry) _angrySlimeSprite = sprite;
            else _calmSlimeSprite = sprite;
            return sprite;
        }

        public static Sprite GetAngerCreatureSprite()
        {
            if (_angerCreatureSprite != null) return _angerCreatureSprite;
            int size = 48;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.36f);
                    float ny = (y - size * 0.45f) / (size * 0.38f);
                    float angle = Mathf.Atan2(ny, nx);
                    float wave = Mathf.Sin(angle * 5f) * 0.12f;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny) - wave;

                    if (dist > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color col = dist < 0.45f
                        ? new Color(1f, 0.95f, 0.55f, 1f)
                        : new Color(1f, 1f, 1f, Mathf.Clamp01((1f - dist) * 3f));

                    float leftEye = Vector2.Distance(new Vector2(x, y), new Vector2(19f, 21f));
                    float rightEye = Vector2.Distance(new Vector2(x, y), new Vector2(29f, 21f));
                    if (leftEye < 2.6f || rightEye < 2.6f)
                    {
                        col = new Color(0.2f, 0.05f, 0.05f, 1f);
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            _angerCreatureSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _angerCreatureSprite.name = "ProceduralAngerCreature";
            return _angerCreatureSprite;
        }

        public static Sprite GetLargeEnemySprite()
        {
            if (_largeEnemySprite != null) return _largeEnemySprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs(x - size * 0.5f) / (size * 0.44f);
                    float ny = (y - size * 0.46f) / (size * 0.44f);

                    bool inHorn = y >= 48 && nx > 0.45f && nx < 0.92f && (y - 48) < (nx - 0.35f) * 28f;
                    bool inBody = nx <= 0.92f && y >= 4 && y <= 50 && (nx * nx * 0.6f + ny * ny) <= 1.05f;

                    if (!inBody && !inHorn)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color col = new Color(0.85f, 0.8f, 0.92f, 1f);
                    if (nx > 0.8f || y < 8 || y > 47)
                        col = new Color(0.55f, 0.48f, 0.65f, 1f);

                    float leftEye = Vector2.Distance(new Vector2(x, y), new Vector2(20f, 32f));
                    float rightEye = Vector2.Distance(new Vector2(x, y), new Vector2(44f, 32f));
                    if (leftEye < 4.5f || rightEye < 4.5f)
                    {
                        col = new Color(1f, 0.2f, 0.25f, 1f);
                    }

                    if (y >= 14 && y <= 20 && x >= 18 && x <= 46)
                    {
                        bool tooth = ((x / 4) % 2 == 0) && y >= 16;
                        col = tooth ? new Color(0.95f, 0.95f, 0.95f, 1f) : new Color(0.12f, 0.06f, 0.16f, 1f);
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            _largeEnemySprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _largeEnemySprite.name = "ProceduralLargeEnemy";
            return _largeEnemySprite;
        }

        public static Sprite GetExitPortalSprite()
        {
            if (_exitPortalSprite != null) return _exitPortalSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.42f);
                    float ny = (y - size * 0.48f) / (size * 0.46f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float ring = Mathf.Sin(r * 14f);
                    float alpha = Mathf.Clamp01((1f - r) * 1.6f);
                    Color c = ring > 0f
                        ? new Color(1f, 1f, 1f, alpha)
                        : new Color(0.7f, 0.95f, 1f, alpha * 0.85f);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            _exitPortalSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _exitPortalSprite.name = "ProceduralExitPortal";
            return _exitPortalSprite;
        }

        public static Sprite GetRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.48f);
                    float ny = (y - size * 0.5f) / (size * 0.48f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r > 1f || r < 0.65f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float alpha = 1f - Mathf.Abs(r - 0.825f) / 0.175f;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
                    }
                }
            }

            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _ringSprite.name = "ProceduralRing";
            return _ringSprite;
        }
    }
}
