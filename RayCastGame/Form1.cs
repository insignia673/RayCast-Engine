using RayCaster.Utils;
using RayCastGame.Models;
using RayCastGame.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RayCastGame
{
    public partial class Form1 : Form
    {
        float fov = 1;
        public float FOV
        {
            get => fov;
            set
            {
                if (value > 0.2f && value < 4.5f)
                    fov = value;
            }
        }
        public GameState State { get; set; } = GameState.InGame;

        Map map;
        Player player;
        bool usingCursor = false;

        double gameTime = 0;
        DateTime _lastCheck = DateTime.Now;

        uint[,] buffer;
        uint[][] texture = new uint[8][];
        Bitmap bmp;
        int texWidth = 256;
        int texHeight = 256;

        Sprite[] sprites = new Sprite[0];
        //{
        //    new Sprite{ CoorX = 1.2f, CoorY = 5.6f, Texture = 1 },
        //    new Sprite{ CoorX = 5.6f, CoorY = 1.2f, Texture = 1 },
        //    new Sprite{ CoorX = 7f, CoorY = 7f, Texture = 1 }
        //};
        double[] zBuffer;
        int[] spriteOrder;
        double[] spriteDistance;


        /// <TODO>
        /// implement resolution settings (games buffer doesnt have to be the same resolution as screen res) transfer to monogame
        /// </TODO>

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
            Cursor.Hide();

            KeyDown += (s, e) =>
            {
                Input.KEYINPUT[e.KeyCode] = true;
                if (e.KeyCode == Keys.M)
                    usingCursor = !usingCursor;
            };
            KeyUp += (s, e) =>
            {
                Input.KEYINPUT[e.KeyCode] = false;
            };

            MouseWheel += (s, e) =>
            {
                if (e.Delta > 0)
                {
                    FOV += .1f;
                }
                else FOV += -0.1f;
            };

            for (int i = 0; i < 8; i++)
                texture[i] = new uint[texWidth * texHeight];

            #region Other Textures
            //for (int x = 0; x < texWidth; x++)
            //    for (int y = 0; y < texHeight; y++)
            //    {
            //        int xorcolor = (x * 256 / texWidth) ^ (y * 256 / texHeight);
            //        //int xcolor = x * 256 / texWidth;
            //        int ycolor = y * 256 / texHeight;
            //        int xycolor = y * 128 / texHeight + x * 128 / texWidth;
            //        texture[0][64 * y + x] = (uint)(65536 * 254 * ((x != y && x != texWidth - y) ? 1 : 0)); //flat red texture with black cross
            //        texture[1][texWidth * y + x] = (uint)(xycolor + 256 * xycolor + 65536 * xycolor); //sloped greyscale
            //        texture[2][texWidth * y + x] = (uint)(256 * xycolor + 65536 * xycolor); //sloped yellow gradient
            //        texture[3][texWidth * y + x] = (uint)(xorcolor + 256 * xorcolor + 65536 * xorcolor); //xor greyscale
            //        texture[4][texWidth * y + x] = (uint)(256 * xorcolor); //xor green
            //        texture[5][texWidth * y + x] = (uint)(65536 * 192 * ((x % 16 == 0 && y % 16 == 0) ? 1 : 0)); //red bricks
            //        texture[6][texWidth * y + x] = (uint)(65536 * ycolor); //red gradient
            //        texture[7][texWidth * y + x] = 128 + 256 * 128 + 65536 * 128; //flat grey texture
            //    }
            #endregion

            LoadTexture(@"Textures\wall.png", 0);
            //LoadTexture(@"Textures\", 1);
            LoadTexture(@"Textures\floor.jpg", 2);
            LoadTexture(@"Textures\ceiling.jpg", 3);

            zBuffer = new double[ClientRectangle.Width];
            spriteOrder = new int[3];
            spriteDistance = new double[3];

            map = new Map(15, 15);
            player = new Player();
            player.CoorX = 1.1f;
            player.CoorY = 1.1f;
        }
        private static uint ToUint(Color c)
        {
            return (uint)(((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B) & 0xffffffffL);
        }
        private void LoadTexture(string val, int index)
        {
            var img = Image.FromFile(val) as Bitmap;

            for (int i = 0; i < texWidth; i++)
            {
                for (int j = 0; j < texHeight; j++)
                {
                    texture[index][texWidth * j + i] = ToUint(img.GetPixel(i, j));
                }
            }
        }

        private void GameUpdate(object sender, PaintEventArgs e)
        {
            gameTime = (DateTime.Now - _lastCheck).TotalSeconds;
            _lastCheck = DateTime.Now;
            
            Input.Mouse = MousePosition;
            
            switch (State)
            {
                case GameState.MainMenu:
                    DrawMenu(e);
                    break;
                case GameState.Creator:
                    DrawCreator(e);
                    break;
                case GameState.Settings:
                    DrawSettings(e);
                    break;
                case GameState.InGame:
                    if (usingCursor)
                        Cursor.Position = new Point(Left + 200, Top + 200);
                    player.Update(gameTime, map.World, Input.Mouse.X - MousePosition.X);
                    zBuffer = new double[ClientRectangle.Width];
                    buffer = new uint[ClientRectangle.Height, ClientRectangle.Width];// y-coordinate first because it works per scanline (FIX THIS, might not be nessessary to update eachtime)
                    bmp = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
                    
                    DrawGame(e);
                    break;
            }

            Invalidate();
        }

        private void DrawSettings(PaintEventArgs e)
        {
        }

        private void DrawCreator(PaintEventArgs e)
        {
        }

        private void DrawMenu(PaintEventArgs e)
        {
        }

        private void DrawGame(PaintEventArgs e)
        {
            SetBuffer();

            unsafe
            {
                fixed (uint* ptr = &buffer[0, 0])
                {
                    bmp = new Bitmap(bmp.Width, bmp.Height, bmp.Width * 4, PixelFormat.Format32bppArgb, (IntPtr)ptr);
                }
            }
            e.Graphics.DrawImage(bmp, 0, 0);
        }

        private void SetBuffer()
        {
            var posX = player.CoorX;
            var posY = player.CoorY;

            //FLOOR
            for (int y = 0; y < ClientRectangle.Height; y++)
            {
                // rayDir for leftmost ray (x = 0) and rightmost ray (x = w)
                float rayDirX0 = (float)(player.DirX - player.PlaneX * -fov);
                float rayDirY0 = (float)(player.DirY - player.PlaneY * -fov);
                float rayDirX1 = (float)(player.DirX + player.PlaneX * -fov);
                float rayDirY1 = (float)(player.DirY + player.PlaneY * -fov);

                // Current y position compared to the center of the screen (the horizon)
                int p = y - ClientRectangle.Height / 2;

                // Vertical position of the camera.
                float posZ = 0.5f * ClientRectangle.Height;

                // Horizontal distance from the camera to the floor for the current row.
                // 0.5 is the z position exactly in the middle between floor and ceiling.
                float rowDistance = posZ / p / fov;

                // calculate the real world step vector we have to add for each x (parallel to camera plane)
                // adding step by step avoids multiplications with a weight in the inner loop
                float floorStepX = rowDistance * (rayDirX1 - rayDirX0) / ClientRectangle.Width;
                float floorStepY = rowDistance * (rayDirY1 - rayDirY0) / ClientRectangle.Width;

                // real world coordinates of the leftmost column. This will be updated as we step to the right.
                float floorX = posX + rowDistance * rayDirX0;
                float floorY = posY + rowDistance * rayDirY0;

                for (int x = 0; x < ClientRectangle.Width; ++x)
                {
                    // the cell coord is simply got from the integer parts of floorX and floorY
                    int cellX = (int)(floorX);
                    int cellY = (int)(floorY);

                    // get the texture coordinate from the fractional part
                    int tx = (int)(texWidth * (floorX - cellX)) & (texWidth - 1);
                    int ty = (int)(texHeight * (floorY - cellY)) & (texHeight - 1);

                    floorX += floorStepX;
                    floorY += floorStepY;

                    // choose texture and draw the pixel
                    uint color;

                    // floor
                    color = texture[2][texWidth * ty + tx];
                    color = (color >> 1) & 8355711; // make a bit darker
                    buffer[y, x] = color + 4278190080;

                    //ceiling (symmetrical, at screenHeight - y - 1 instead of y)
                    color = texture[3][texWidth * ty + tx];
                    color = (color >> 1) & 8355711; // make a bit darker
                    buffer[ClientRectangle.Height - y - 1, x] = color + 4278190080;
                }
            }

            //WALLS
            for (int x = 0; x < ClientRectangle.Width; x++)
            {
                double cameraX = 2 * x / (double)ClientRectangle.Width - 1;
                double rayDirX = player.DirX + player.PlaneX * cameraX * -fov;
                double rayDirY = player.DirY + player.PlaneY * cameraX * -fov;

                int mapX = (int)posX;
                int mapY = (int)posY;

                //length of ray from current position to next x or y-side
                double sideDistX;
                double sideDistY;

                //length of ray from one x or y-side to next x or y-side
                double deltaDistX = (rayDirX == 0) ? 1e30 : Math.Abs(1 / rayDirX);
                double deltaDistY = (rayDirY == 0) ? 1e30 : Math.Abs(1 / rayDirY);
                double perpWallDist;

                //what direction to step in x or y-direction (either +1 or -1)
                int stepX;
                int stepY;

                int hit = 0; //was there a wall hit?
                int side = 0; //was a NS or a EW wall hit?

                //calculate step and initial sideDist
                if (rayDirX < 0)
                {
                    stepX = -1;
                    sideDistX = (posX - mapX) * deltaDistX;
                }
                else
                {
                    stepX = 1;
                    sideDistX = (mapX + 1.0 - posX) * deltaDistX;
                }
                if (rayDirY < 0)
                {
                    stepY = -1;
                    sideDistY = (posY - mapY) * deltaDistY;
                }
                else
                {
                    stepY = 1;
                    sideDistY = (mapY + 1.0 - posY) * deltaDistY;
                }

                //perform DDA
                while (hit == 0)
                {
                    //jump to next map square, either in x-direction, or in y-direction
                    if (sideDistX < sideDistY)
                    {
                        sideDistX += deltaDistX;
                        mapX += stepX;
                        side = 0;
                    }
                    else
                    {
                        sideDistY += deltaDistY;
                        mapY += stepY;
                        side = 1;
                    }
                    //Check if ray has hit a wall
                    if (map.World[mapX, mapY] > 0) hit = 1;
                }

                //Calculate distance projected on camera direction (Euclidean distance would give fisheye effect!)
                if (side == 0) perpWallDist = (sideDistX - deltaDistX) * fov;
                else perpWallDist = (sideDistY - deltaDistY) * fov;

                //Calculate height of line to draw on screen
                int lineHeight = (int)(ClientRectangle.Height / perpWallDist);

                //calculate lowest and highest pixel to fill in current stripe
                int drawStart = -lineHeight / 2 + ClientRectangle.Height / 2;
                if (drawStart < 0) drawStart = 0;
                int drawEnd = lineHeight / 2 + ClientRectangle.Height / 2;
                if (drawEnd >= ClientRectangle.Height) drawEnd = ClientRectangle.Height - 1;

                //Buffer
                //------------------------------------
                //texturing calculations
                int texNum = map.World[mapX, mapY] - 1; //1 subtracted from it so that texture 0 can be used!

                //calculate value of wallX
                double wallX; //where exactly the wall was hit
                if (side == 0) wallX = posY + perpWallDist * rayDirY / fov;
                else wallX = posX + perpWallDist * rayDirX / fov;
                wallX -= Math.Floor(wallX);

                //x coordinate on the texture
                int texX = (int)(wallX * (double)texWidth);
                if (side == 0 && rayDirX > 0) texX = texWidth - texX - 1;
                if (side == 1 && rayDirY < 0) texX = texWidth - texX - 1;

                // How much to increase the texture coordinate per screen pixel
                double step = 1.0 * texHeight / lineHeight;
                // Starting texture coordinate
                double texPos = (drawStart - ClientRectangle.Height / 2 + lineHeight / 2) * step;
                for (int y = drawStart; y < drawEnd; y++)
                {
                    // Cast the texture coordinate to integer, and mask with (texHeight - 1) in case of overflow
                    int texY = (int)texPos & (texHeight - 1);
                    texPos += step;
                    uint color = texture[texNum][texHeight * texY + texX];
                    //make color darker for y-sides: R, G and B byte each divided through two with a "shift" and an "and"
                    if (side == 1) color = (color >> 1) & 8355711;
                    buffer[y, x] = color + 4278190080;
                }

                //SET THE ZBUFFER FOR THE SPRITE CASTING
                zBuffer[x] = perpWallDist; //perpendicular distance is used
            }

            //SPRITE CASTING
            //sort sprites from far to close
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteOrder[i] = i;
                spriteDistance[i] = (posX - sprites[i].CoorX) * (posX - sprites[i].CoorX) + (posY - sprites[i].CoorY) * (posY - sprites[i].CoorY); //sqrt not taken, unneeded
            }
            sortSprites();

            //after sorting the sprites, do the projection and draw them
            for (int i = 0; i < sprites.Length; i++)
            {
                //translate sprite position to relative to camera
                double spriteX = sprites[spriteOrder[i]].CoorX - posX;
                double spriteY = sprites[spriteOrder[i]].CoorY - posY;

                //transform sprite with the inverse camera matrix
                // [ planeX   dirX ] -1                                       [ dirY      -dirX ]
                // [               ]       =  1/(planeX*dirY-dirX*planeY) *   [                 ]
                // [ planeY   dirY ]                                          [ -planeY  planeX ]

                double invDet = 1.0 / (player.PlaneX * player.DirY - player.DirX * player.PlaneY) * fov; //required for correct matrix multiplication

                double transformX = invDet * (player.DirY * spriteX - player.DirX * spriteY) / -fov;
                double transformY = invDet * (-player.PlaneY * spriteX + player.PlaneX * spriteY); //this is actually the depth inside the screen, that what Z is in 3D

                int spriteScreenX = (int)(ClientRectangle.Width / 2 * (1 + transformX / transformY));

                //calculate height of the sprite on screen
                int spriteHeight = Math.Abs((int)(ClientRectangle.Height / transformY)); //using 'transformY' instead of the real distance prevents fisheye
                                                                                         //calculate lowest and highest pixel to fill in current stripe
                int drawStartY = -spriteHeight / 2 + ClientRectangle.Height / 2;
                if (drawStartY < 0) drawStartY = 0;
                int drawEndY = spriteHeight / 2 + ClientRectangle.Height / 2;
                if (drawEndY >= ClientRectangle.Height) drawEndY = ClientRectangle.Height - 1;

                //calculate width of the sprite
                int spriteWidth = (int)(ClientRectangle.Height / transformY); // this originally was inside Math.Abs but it seems to work without it. change if needed
                int drawStartX = -spriteWidth / 2 + spriteScreenX;
                if (drawStartX < 0) drawStartX = 0;
                int drawEndX = spriteWidth / 2 + spriteScreenX;
                if (drawEndX >= ClientRectangle.Width) drawEndX = ClientRectangle.Width - 1;

                //loop through every vertical stripe of the sprite on screen
                for (int stripe = drawStartX; stripe < drawEndX; stripe++)
                {
                    int texX = 256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * texWidth / spriteWidth / 256;
                    //the conditions in the if are:
                    //1) it's in front of camera plane so you don't see things behind you
                    //2) it's on the screen (left)
                    //3) it's on the screen (right)
                    //4) ZBuffer, with perpendicular distance
                    if (transformY > 0 && stripe > 0 && stripe < ClientRectangle.Width && transformY < zBuffer[stripe])
                        for (int y = drawStartY; y < drawEndY; y++) //for every pixel of the current stripe
                        {
                            int d = y * 256 - ClientRectangle.Height * 128 + spriteHeight * 128; //256 and 128 factors to avoid floats
                            int texY = (d * texHeight) / spriteHeight / 256;
                            uint color = texture[sprites[spriteOrder[i]].Texture][Math.Abs(texWidth * texY + texX)]; //get current color from the texture
                            if (color != 0x00FFFFFF) buffer[y, stripe] = color; //paint pixel if it isn't invisible
                        }
                }
            }
        }
        void sortSprites()
        {
            (double, int)[] temp = new (double, int)[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                temp[i].Item1 = spriteDistance[i];
                temp[i].Item2 = spriteOrder[i];
            }
            Array.Sort(temp);
            // restore in reverse order to go from farthest to nearest
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteDistance[i] = temp[sprites.Length - i - 1].Item1;
                spriteOrder[i] = temp[sprites.Length - i - 1].Item2;
            }
        }
    }
}
