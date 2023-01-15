using Microsoft.AspNetCore.Mvc;

namespace Spolis.Controllers
{
    public abstract class HomeController : hIndexController<PersonViewModel>
    {
        public HomeController()
        {
        }
        public override IActionResult Index([FromQuery] bool showLayout = true)
        {
            return base.Index(showLayout);
        }
        public override IActionResult Edit(Guid id, string message)
        {
            return base.Edit(id, message);
        }
    }
}