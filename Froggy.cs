using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


namespace Froggy;

/// @author Marianne Ylönen
/// @version 23.02.2024
/// <summary>
/// Kyseessä peliohjelma joka luo pelihahmoksi sammakon, jota voi ohjailla
/// nuolinäppäimmillä. Pelin tarkoitus on kerätä mahdollisimman monta hyönteistä
/// osumatta liikkuvaan kissaan. Kissaan osuminen lopettaa pelin.
///
/// english:
/// This is a game program that creates a frog as the player character, controlled 
/// by arrow keys. The goal of the game is to collect as many insects as possible 
/// without colliding with a moving cat. Colliding with the cat ends the game.
/// </summary>
public class Froggy : PhysicsGame
{
    private double liikkumisnopeus = 500;
    private Image sammakkoKuva;
    private PhysicsObject sammakkohahmo;
    private Image perhonenKuva;
    private PhysicsObject perhonenhahmo;
    private Image perhonenKuva1;
    private Image perhonenKuva2;
    private Image perhonenKuva3;
    private Image hamahakkiKuva;
    private Image leppakerttuKuva;
    private Image hottiainenKuva;
    private Image otokkaKuva;
    private PhysicsObject otokkahahmo;
    private Image kisseKuva;
    private PhysicsObject kissehahmo; 
    private IntMeter pistelaskuri;
    private ScoreList topLista = new ScoreList(10, false, 0);

    /// <summary>
    /// Tämä ohjelma tuo yhteen kentän, taustan, ohjaimet sekä pistelaskurin.
    ///
    /// english:
    /// This program sets up the field, background, controls, and score counter.
    /// </summary>
    public override void Begin()
    {
        LuoKentta();
        AsetaOhjaimet();
        Tausta();
        LuoPistelaskuri();
        
        topLista = DataStorage.TryLoad<ScoreList>(topLista, "pisteet.xml");
    }

    
    /// <summary>
    /// LuoKenttä-ohjelma luo rajat sekä kutsuu eri hahmoja jotka määritelty aiemmin.
    ///
    /// english:
    /// Creates borders and spawns various characters defined earlier.
    /// </summary>
    private void LuoKentta()
    {
        Level.CreateBorders(0, false);
        Sammakko();
        Perhonen(5);
        Otokat(5);
        Kisse();
    }

    
    /// <summary>
    /// Ohjelma luo taustan: lataa taustakuvan, taustamusiikin sekä määrittää peli-ikkunan koon.
    /// Lisäksi ohjelma luo ohjeikkunan jossa neuvotaan pelin kulku.
    ///
    /// english:
    /// This function creates the background: loads a background image, background music, 
    /// and defines the game window size. Additionally, it creates an instruction window 
    /// that explains how the game works.
    /// </summary>
    private void Tausta()
    {
        Image taustaKuva = LoadImage("tausta.jpg");
        MediaPlayer.Play("taustaAani.wav");
        MediaPlayer.IsRepeating = true;
        Level.Background.Image = taustaKuva;
        Level.Size = new Vector(950, 800);
        SetWindowSize(950, 800);
        MessageDisplay.Add("Pelin tarkoitus on kerätä mahdollisimman monta hyönteistä osumatta kissaan!");
        MessageDisplay.Add("Kissaan törmättyäsi peli päättyy!");
        MessageDisplay.MessageTime = new TimeSpan(0, 0, 10);
    }

    
    /// <summary>
    /// AsetaOhjaimet-ohjelma luo näppäimmistökomennot nuolinäppäimmille,
    /// tällä ohjelmalla liikutetaan sammakko-hahmoa.
    ///
    /// english:
    /// Function creates keyboard commands for the arrow keys to move the frog character.
    /// </summary>
    private void AsetaOhjaimet()
    {
        {   
            Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, null, sammakkohahmo, new Vector(-liikkumisnopeus, 0));
            Keyboard.Listen(Key.Left, ButtonState.Released, Liikuta, null, sammakkohahmo, Vector.Zero);
            Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, null, sammakkohahmo, new Vector(liikkumisnopeus, 0));
            Keyboard.Listen(Key.Right, ButtonState.Released, Liikuta, null, sammakkohahmo, Vector.Zero);
            Keyboard.Listen(Key.Down, ButtonState.Down, Liikuta, null, sammakkohahmo, new Vector(0, -liikkumisnopeus));
            Keyboard.Listen(Key.Down, ButtonState.Released, Liikuta, null, sammakkohahmo, Vector.Zero);
            Keyboard.Listen(Key.Up, ButtonState.Down, Liikuta, null, sammakkohahmo, new Vector(0, liikkumisnopeus));
            Keyboard.Listen(Key.Up, ButtonState.Released, Liikuta, null, sammakkohahmo, Vector.Zero);

            Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
            Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
           
        }
    }

    
    /// <summary>
    /// Ohjelma luo pistelaskurin ja pistenäytön.
    ///
    /// english:
    /// This function creates the score counter and its display.
    /// </summary>
    private void LuoPistelaskuri()
    {  
        pistelaskuri = new IntMeter(0);
        Label pistenaytto = new Label();
        pistenaytto.X = Screen.Left + 100;
        pistenaytto.Y = Screen.Top - 100;
        pistenaytto.TextColor = Color.Black;
        pistenaytto.Color = Color.LightGreen;

        pistenaytto.BindTo(pistelaskuri);
        pistenaytto.Title = "Pisteet : ";
        Add(pistenaytto);
    }

    
    /// <summary>
    /// Liikuta-ohjelma liikuttaa Sammakkohahmoa sekä määrittää painovoiman.
    ///
    /// english:
    /// The Move function moves the frog character and defines gravity.
    /// </summary>
    /// <param name="sammakko">pelihahmo/the player character</param> 
    /// <param name="suunta">mihin liikkuu/movement direction</param>
    private void Liikuta(PhysicsObject sammakko, Vector suunta)
    {   
        sammakkohahmo.Velocity = suunta;
        Gravity = new Vector(0.0, -800.0);
    }
    
    
    /// <summary>
    /// Ohjelma luo Sammakko-pelihahmon jota ohjaillaan. Ohjelmassa myös määritellään
    /// mihin sammakko sijoittuu ja luodaan pohjaa SammakkoSyö-aliohjelmalle,
    ///
    /// english:
    /// This function creates the frog character that is controlled by the player. 
    /// It also defines the frog's initial position and sets up the collision handlers 
    /// for when the frog eats insects or collides with the cat.
    /// </summary>
    private void Sammakko()
    {
        sammakkoKuva = LoadImage("sammakko.png");
        sammakkohahmo = new PhysicsObject(150, 150);
        sammakkohahmo.Mass = 2000;
        sammakkohahmo.Image = sammakkoKuva;
        sammakkohahmo.Y = sammakkohahmo.Y - 400;
        sammakkohahmo.Tag = "Sammakko";
        AddCollisionHandler(sammakkohahmo, "perhonen", SammakkoSyo);
        AddCollisionHandler(sammakkohahmo, "ötökkä", SammakkoSyo);
        AddCollisionHandler(sammakkohahmo, "kissa", SammakkoSyo);
        Add(sammakkohahmo);
    }

    
    /// <summary>
    /// Seuraava aliohjelma luo pelikentälle muita hahmoja joita tekoäly liikuttaa satunnaisesti
    ///
    /// english:
    /// This function creates butterfly characters on the game field, 
    /// which are moved randomly by AI.
    /// </summary>
    /// <param name="maara">perhosten lukumäärä/number of butterflies</param>
    private void Perhonen(int maara)
    {
        int perhonen = 0;
        while (perhonen < maara)
        {
            perhonenKuva1 = LoadImage("perhonen1.png");
            perhonenKuva2 = LoadImage("perhonen2.png");
            perhonenKuva3 = LoadImage("perhonen3.png");
            Image[] perhosKuvat = { perhonenKuva1, perhonenKuva2, perhonenKuva3 };
            perhonenKuva = RandomGen.SelectOne(perhosKuvat);
            perhonenhahmo = new PhysicsObject(80, 80);
            perhonenhahmo.Image = perhonenKuva;
            perhonenhahmo.Y = AnnaSijainti(perhonenhahmo);
            perhonenhahmo.Tag = "perhonen";
            RandomMoverBrain satunnaisaivot = new RandomMoverBrain(200);
            satunnaisaivot.ChangeMovementSeconds = 1;
            perhonenhahmo.Brain = satunnaisaivot;
            perhonenhahmo.IgnoresGravity = true;
            satunnaisaivot.TurnWhileMoving = true;
            Add(perhonenhahmo);
            perhonen++;
        }
    }
    
    
    /// <summary>
    /// Seuraava aliohjelma luo pelikentälle kerättäviä ötököitä joita tekoäly liikuttaa satunnaisesti.
    ///
    /// english:
    /// This function creates collectible bugs on the game field, 
    /// which are moved randomly by AI.
    /// </summary>
    /// <param name="maara">ötököitten lukumäärä/number of bugs</param>
    private void Otokat(int maara)
    {
        int otokka = 0;
        while (otokka < maara)
        {
            hamahakkiKuva = LoadImage("hamahakki.png");
            leppakerttuKuva = LoadImage("leppakerttu.png");
            hottiainenKuva = LoadImage("hottiainen.png");
            Image[] otokkaKuvat = { hamahakkiKuva, leppakerttuKuva, hottiainenKuva };
            otokkaKuva = RandomGen.SelectOne(otokkaKuvat);
            otokkahahmo = new PhysicsObject(80, 80);
            otokkahahmo.Image = otokkaKuva;
            otokkahahmo.Y = AnnaSijainti(otokkahahmo);
            otokkahahmo.Tag = "ötökkä";
            RandomMoverBrain satunnaisaivot = new RandomMoverBrain(200);
            satunnaisaivot.ChangeMovementSeconds = 3;
            otokkahahmo.Brain = satunnaisaivot;
            otokkahahmo.IgnoresGravity = true;
            satunnaisaivot.TurnWhileMoving = true;
            Add(otokkahahmo);
            otokka++;
        }
    }

    
    /// <summary>
    /// Aliohjelma määrittää satunnaisen sijainnin hahmoille, jotta ne eivät kaikki ilmesty samaan paikkaan.
    ///
    /// english:
    /// This function defines a random spawn location for characters 
    /// to prevent them from appearing in the same spot.
    /// </summary>
    /// <param name="hahmo">hahmo jota sijoitetaan/the character to position</param>
    /// <returns></returns>
    public static int AnnaSijainti(PhysicsObject hahmo)
    {
        var random = new Random();
        int y = random. Next(100, 500);
        return y;
    }
    
    
    /// <summary>
    /// Aliohjelma luo kissahahmon, joka liikkuu satunnaisesti.
    ///
    /// english:
    /// This function creates the cat character, which moves randomly.
    /// </summary>
    private void Kisse()
    {
        kisseKuva = LoadImage("kisse.png");
        kissehahmo = new PhysicsObject(120, 120);
        kissehahmo.Mass = 1000;
        kissehahmo.Image = kisseKuva;
        kissehahmo.Y = kissehahmo.Y + 400;
        kissehahmo.Tag = "kissa";
        RandomMoverBrain satunnaisaivot = new RandomMoverBrain(200);
        satunnaisaivot.ChangeMovementSeconds = 2;
        kissehahmo.Brain = satunnaisaivot;
        Add(kissehahmo);
    }
    
    
    /// <summary>
    /// /Ohjelma määrittää törmäyksen ja seurauksen mikä törmäyksestä seuraa:mikäli kyseessä hyönteinen,
    /// pelaaja saa pisteitä, mikäli osuu kissaan peli päättyy ja avautuu parhaat pisteet-näkymä. Määrittää myös
    /// pisteiden tallentamisen.
    ///
    /// english:
    /// This function handles collisions and their consequences: 
    /// if the player collides with an insect, points are awarded; 
    /// if the player collides with the cat, the game ends and the high score window appears.
    /// It also handles score saving.
    /// </summary>
    /// <param name="tormaaja">sammakko/kissa / frog/cat</param>
    /// <param name="kohde">ötökkä/perhonen / bug/butterfly</param>
    public void SammakkoSyo(PhysicsObject tormaaja, PhysicsObject kohde)
    {    

        if (kohde.Tag.ToString() == "ötökkä")
        {
            kohde.Destroy();
            pistelaskuri.Value += 1;
            Otokat(1);
        }

        if (kohde.Tag.ToString() == "perhonen")
        {
            kohde.Destroy();
            pistelaskuri.Value += 5;
            Perhonen(1);
        }
        
        if (kohde.Tag.ToString() == "kissa")
        {
            tormaaja.Destroy();
            HighScoreWindow topIkkuna = new HighScoreWindow(
                "Onneksi olkoon!",
                "Voi ei kissa sai sinut kiinni, mutta pääsit listalle pisteillä %p! Syötä nimesi:",
                topLista, pistelaskuri);
            topIkkuna.Closed += TallennaPisteet;
            topIkkuna.Closed += AloitaAlusta;
            Add(topIkkuna);
        }
        void TallennaPisteet(Window sender) 
        {
            DataStorage.Save<ScoreList>(topLista, "pisteet.xml");
        }
    }

    
    /// <summary>
    /// Aloittaa pelin alusta kun pelaaja kuolee kissaan törmättyään.
    ///
    /// english:
    /// Restarts the game after the player dies by colliding with the cat.
    /// </summary>
    /// <param name="sender">avaa ikkunan/ opens the window</param>
    public void AloitaAlusta(Window sender)
    {
        ClearAll();
        LuoKentta();
        AsetaOhjaimet();
        Tausta();
        LuoPistelaskuri();
        
        topLista = DataStorage.TryLoad<ScoreList>(topLista, "pisteet.xml");
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }
}
