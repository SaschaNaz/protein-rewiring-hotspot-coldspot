using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using System.Xml.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

/* 각 코드를 만들면 한번 쓰고 버리는 지금 방식에서
 * 어떤 작업을 하면 거기에 필요한 구성 성분을 각각 생성하고선 이를 전역 변수로 저장하고, 그 여부를 UI로 알 수 있도록 한다
 * 매번 다시 연산하지 않고 전역 변수에 저장된 걸 사용
 * 필요한 게 없으면 생성한다
 * 불러오는 함수를 만들자, 없으면 만들어서 저장한 뒤 리턴하고 있으면 그대로 리턴하는 함수 - 최대한 코드를 기존 그대로 쓸 수 있게끔
 * UI에서 파일저장 작업 자유롭게 할 수 있도록 만들기
 */

namespace Life302
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DataManager manager = new DataManager();

        public MainPage()
        {
            this.InitializeComponent();
            Process();
        }

        async void Process()
        {
            //await saveDrosophilaNetwork();
            //await saveDrosophilaToHumanOrtholog();
            //await saveMappedOrtholog();
            //await saveUniprotMapper();
            await manager.saveHumanNetwork();

            //var ortholog = await readDrosophilaToHumanOrtholog();

            //await saveRValue();

            //await saveValidOrtholog();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
    }
}
