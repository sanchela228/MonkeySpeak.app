using App;
using App.Scenes;

Context.Instance.SetUp();

using var window = new App.Window();
window.Run( new StartUp() );