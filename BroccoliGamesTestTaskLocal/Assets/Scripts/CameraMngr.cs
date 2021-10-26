
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEditor;

public class CameraMngr : MonoBehaviour
{
    private new Camera camera;
    private Vector2 mouseStartPos;

    public SpritesList spritesListObj;
    private string file = "testing_views_settings.json";

    private GameObject topLeftSprite;

    //коллекция спрайтов для создания карты
    [SerializeField]
    private List<Sprite> mapSprites;
    [SerializeField]
    private GameObject mapSpritePrefab; //префаб игрового объекта для назначения спрайтов
    private List<SpriteData> spriteDataObjs; //колллекция со спрайтов в качестве ключа и объектов данных в качестве значения

    //параметры приближения и отдаления карты 
    private const float maxZoomIn = 0;
    private const float maxZoomOut = 6;

    //параметры границ карты для уровня максимального и минимального приближения, при этом чтобы камера не выходила за границы карты
    private float XMaxBorder;
    private float XMinBorder;
    private float YMaxBorder;
    private float YMinBorder;

    //переменные для хранения переметров отображаемого экрана
    private float vertScreenSize;
    private float horisScreenSize;

    //параметры плавной остановки движения карты после прокручивания его игроком
    [SerializeField]
    private float lerpTime;
    private float lerpToPosX;
    private float lerpToPosY;

    //переменные для хранения координат нажатой кнопки
    private float currentMousePosX;
    private float currentMousePosY;

    //данные UI
    [SerializeField]
    private GameObject infoPanel;
    [SerializeField]
    private Text IdTxt;
    [SerializeField]
    private Text TypeTxt;
    [SerializeField]
    private Text XTxt;
    [SerializeField]
    private Text YTxt;
    [SerializeField]
    private Text WTxt;
    [SerializeField]
    private Text HTxt;
    private bool panelIsOpen;


    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
        lerpToPosX = transform.position.x;
        lerpToPosY = transform.position.y;
        panelIsOpen = false;

        //получаем данные из файла
        string json = ReadDataFromJsonFile(file);
        spritesListObj = JsonUtility.FromJson<SpritesList>(json);

        //создаем коллекцию для хранения выгруженных из файла объектов
        spriteDataObjs = new List<SpriteData>();

        //создаем на сцене игровые объекты с соотвествующими спрайтами
        for (int i = 0; i < mapSprites.Count; i++)
        {
            GameObject spriteObj = Instantiate(mapSpritePrefab, new Vector3(spritesListObj.List[i].X, spritesListObj.List[i].Y, 0), Quaternion.identity);
            spriteObj.GetComponent<SpriteRenderer>().sprite = mapSprites[i];
            if (i == 0) topLeftSprite = spriteObj;
        }
        //заполняем коллекцию данных для отображения информации по спрайту при нажатии кнопки
        for (int i = 0; i < spritesListObj.List.Length; i++)
        {
            spriteDataObjs.Add(spritesListObj.List[i]);
        }

        //определяем размеры отображаемого экрана
        vertScreenSize = camera.orthographicSize * 2;
        horisScreenSize = vertScreenSize * Screen.width / Screen.height;

        //определяем границы для движения камеры по карте
        XMinBorder = topLeftSprite.GetComponent<SpriteRenderer>().bounds.size.x / 2 * -1;
        XMaxBorder = XMinBorder * -1 * 2 * 7 - XMinBorder;
        YMaxBorder = topLeftSprite.GetComponent<SpriteRenderer>().bounds.size.y / 2;
        YMinBorder = YMaxBorder * -1 * 2 * 2 - YMaxBorder;

        //задаем начальную позицию камеры (верхний левый угол)
        transform.position = new Vector3(XMinBorder + horisScreenSize / 2, YMaxBorder - vertScreenSize / 2, -1);
    }

    //переопределение границ камеры 
    private void reconsiderCameraBorders()
    {   //переопределяем границы для движения камеры по карте при изменении размера камеры
        vertScreenSize = camera.orthographicSize * 2;
        horisScreenSize = vertScreenSize * Screen.width / Screen.height;
        XMinBorder = topLeftSprite.GetComponent<SpriteRenderer>().bounds.size.x / 2 * -1;
        XMaxBorder = XMinBorder * -1 * 2 * 7 - XMinBorder;
        YMaxBorder = topLeftSprite.GetComponent<SpriteRenderer>().bounds.size.y / 2;
        YMinBorder = YMaxBorder * -1 * 2 * 2 - YMaxBorder;

        //сохраняем границы камера в приделах карты при увеличении на границе. 
        if (transform.position.x < (XMinBorder + horisScreenSize / 2))
        {
            currentMousePosX = XMinBorder + horisScreenSize / 2;
            currentMousePosY = transform.position.y;
            lerpToPosX = Mathf.Clamp(transform.position.x - currentMousePosX, XMinBorder + horisScreenSize / 2, XMaxBorder - horisScreenSize / 2);
            lerpToPosY = currentMousePosY;
            transform.position = new Vector3(currentMousePosX, transform.position.y, -1);
        }
        if (transform.position.x > (XMaxBorder - horisScreenSize / 2))
        {
            currentMousePosX = XMaxBorder - horisScreenSize / 2;
            currentMousePosY = transform.position.y;
            lerpToPosX = Mathf.Clamp(transform.position.x + currentMousePosX, XMinBorder + horisScreenSize / 2, XMaxBorder - horisScreenSize / 2);
            lerpToPosY = currentMousePosY;
            transform.position = new Vector3(currentMousePosX, transform.position.y, -1);
        }
        if (transform.position.y > (YMaxBorder - vertScreenSize / 2))
        {
            currentMousePosY = YMaxBorder - vertScreenSize / 2;
            currentMousePosX = transform.position.x;
            lerpToPosY = Mathf.Clamp(transform.position.y - currentMousePosY, YMinBorder + vertScreenSize / 2, YMaxBorder - vertScreenSize / 2);
            lerpToPosX = currentMousePosX;
            transform.position = new Vector3(transform.position.x, currentMousePosY, -1);
        }
        if (transform.position.y < (YMinBorder + vertScreenSize / 2))
        {
            currentMousePosY = YMinBorder + vertScreenSize / 2;
            currentMousePosX = transform.position.x;
            lerpToPosY = Mathf.Clamp(transform.position.y + currentMousePosY, YMinBorder + vertScreenSize / 2, YMaxBorder - vertScreenSize / 2);
            lerpToPosX = currentMousePosX;
            transform.position = new Vector3(transform.position.x, currentMousePosY, -1);
        }
    }

    //метод для чтения данных из файла
    private string ReadDataFromJsonFile(string fileName)
    {
        TextAsset txtAsset = (TextAsset)Resources.Load("testing_views_settings", typeof(TextAsset));
        string JsonFile = txtAsset.text;
        return JsonFile;
    }

    //метод для вызова панели информации 
    public void openInfoPanel(int index)
    {
        //закрываем панель информации о спрайте если индекс кнопки 0
        if (index == 0)
        {
            panelIsOpen = false;//даем сигнал что камеру двигать можно
            infoPanel.SetActive(false);
        }
        //открываем панель информации о спрайте
        else
        {
            panelIsOpen = true; //даем сигнал что нельза камеру двигать
            SpriteData spriteDataObj = determineClosestSpriteToCam(); //получаем объект данных соотвествующий самому близкому спрайту к камере
            IdTxt.text = spriteDataObj.Id;
            TypeTxt.text = spriteDataObj.Type;
            XTxt.text = spriteDataObj.X.ToString("0.00");
            YTxt.text = spriteDataObj.Y.ToString("0.00");
            WTxt.text = spriteDataObj.Width.ToString("0.00");
            HTxt.text = spriteDataObj.Height.ToString("0.00");

            infoPanel.SetActive(true);
        }
    }


    //определение и получение самого близконо спрайта к центру камеры
    private SpriteData determineClosestSpriteToCam() {
        SpriteData spriteDataObj;
        List<float> distancesFormCamera = new List<float>();
        //собираем информацию о расстоянии всех спрайтов от текущей позиции камеры
        for (int i = 0; i < spriteDataObjs.Count; i++) {
            distancesFormCamera.Add((new Vector3 (spriteDataObjs[i].X, spriteDataObjs[i].Y,0) - transform.position).sqrMagnitude);
        }
        //определяем индекс самой короткой дистации и по этому же индексу определяем нужный нам объект с данными спрайта
        spriteDataObj = spriteDataObjs[distancesFormCamera.IndexOf(distancesFormCamera.Min())];
        return spriteDataObj;
    }

    //закрыть приложени
    public void closeTheApp() {
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {
        //приближение и отдаление карты реализовано через ортографический размер камеры
        if (camera.orthographicSize > maxZoomIn && camera.orthographicSize < maxZoomOut && !panelIsOpen)
        {
            camera.orthographicSize += Input.mouseScrollDelta.y * -1;
            if (camera.orthographicSize <= maxZoomIn) camera.orthographicSize = maxZoomIn + 1;
            if (camera.orthographicSize >= maxZoomOut) camera.orthographicSize = maxZoomOut - 1;
        }

        //контроль сохранения границ камеры на карте
        reconsiderCameraBorders();

        //передвижение камеры по карте при нажатии кнопки мыши
        if (Input.GetMouseButtonDown(0)&& !panelIsOpen) mouseStartPos = camera.ScreenToWorldPoint(Input.mousePosition);
        else if (Input.GetMouseButton(0)&&!panelIsOpen)
        {
            currentMousePosX = camera.ScreenToWorldPoint(Input.mousePosition).x - mouseStartPos.x;
            currentMousePosY = camera.ScreenToWorldPoint(Input.mousePosition).y - mouseStartPos.y;
            lerpToPosX = Mathf.Clamp(transform.position.x - currentMousePosX, XMinBorder + horisScreenSize / 2, XMaxBorder - horisScreenSize / 2);
            lerpToPosY = Mathf.Clamp(transform.position.y - currentMousePosY, YMinBorder + vertScreenSize / 2, YMaxBorder - vertScreenSize / 2);
        }
        if (!panelIsOpen) transform.position = new Vector3(Mathf.Lerp(transform.position.x, lerpToPosX, lerpTime * Time.deltaTime), Mathf.Lerp(transform.position.y, lerpToPosY, lerpTime * Time.deltaTime), transform.position.z);
    }
}
