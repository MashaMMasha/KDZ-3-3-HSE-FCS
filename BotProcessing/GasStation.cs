using System;

namespace BotProcessing;

public class GasStation
{
    // Поля класса
    private string _id;
    private string _fullName;
    private string _globalId;
    private string _shortName;
    private string _admArea;
    private string _district;
    private string _address;
    private string _owner;
    private string _testDate;
    private string _geoDataCenter;
    private string _geoArea;
    
    public string Id
    {
        get => _id;
        set => _id = value;
    }
    public string FullName
    {
        get => _fullName;
        set => _fullName = value;
    }
    public string GlobalId
    {
        get => _globalId;
        set => _globalId = value;
    }

    public string ShortName
    {
        get => _shortName;
        set => _shortName = value;
    }

    public string AdmArea
    {
        get => _admArea;
        set => _admArea = value;
    }

    public string District
    {
        get => _district;
        set => _district = value;
    }

    public string Address
    {
        get => _address;
        set => _address = value;
    }

    public string Owner
    {
        get => _owner;
        set => _owner = value;
    }

    public string TestDate
    {
        get => _testDate;
        set => _testDate = value;
    }

    public string GeoDataCenter
    {
        get => _geoDataCenter;
        set => _geoDataCenter = value;
    }

    public string GeoArea
    {
        get => _geoArea;
        set => _geoArea = value;
    }
    
    // Конструктор без параметров
    public GasStation()
    {
        _id = "";
        _fullName = "";
        _globalId = "";
        _shortName = "";
        _admArea = "";
        _district = "";
        _address = "";
        _owner = "";
        _testDate = "";
        _geoDataCenter = "";
        _geoArea = "";
    }
    // Конструктор с параметрами
    public GasStation(string id, string fullName, string globalId, string shortName, string admArea, string district, string address, string owner, string testDate, string geoDataCenter, string geoArea)
    {
        _id = id;
        _fullName = fullName;
        _globalId = globalId;
        _shortName = shortName;
        _admArea = admArea;
        _district = district;
        _address = address;
        _owner = owner;
        _testDate = testDate;
        _geoDataCenter = geoDataCenter;
        _geoArea = geoArea;
    }

}