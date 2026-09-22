namespace AngleSharp.Js.Tests
{
    using AngleSharp.Scripting;
    using NUnit.Framework;
    using System.Threading.Tasks;

    [TestFixture]
    public class DatasetTests
    {
        [TestCase("JSON.stringify([d.missing, 'missing' in d, Object.hasOwn(d,'missing'), Object.getOwnPropertyDescriptor(d,'missing')])", "[null,false,false,null]")]
        [TestCase("d.first='1';d.second='2';JSON.stringify([Object.keys(d),Object.assign({},d),{...d}])", "[[\"first\",\"second\"],{\"first\":\"1\",\"second\":\"2\"},{\"first\":\"1\",\"second\":\"2\"}]")]
        [TestCase("d.first='1';var result=delete d.first;JSON.stringify([result,document.body.hasAttribute('data-first'),'first' in d])", "[true,false,false]")]
        [TestCase("d.constructor='ctor';JSON.stringify([d.constructor,Object.keys(d)])", "[\"ctor\",[\"constructor\"]]")]
        [TestCase("d.first=null;d.second=undefined;JSON.stringify([d.first,d.second])", "[\"null\",\"undefined\"]")]
        [TestCase("d.first='1';JSON.stringify(Object.getOwnPropertyDescriptor(d,'first'))", "{\"value\":\"1\",\"writable\":true,\"enumerable\":true,\"configurable\":true}")]
        [TestCase("var s=Symbol('key');d[s]=42;JSON.stringify([d[s],Object.keys(d),Reflect.ownKeys(d).includes(s),delete d[s],s in d])", "[42,[],true,true,false]")]
        [TestCase("try{d.first=Symbol()}catch(e){e.name}", "TypeError")]
        [TestCase("Object.defineProperty(d,'first',{value:'1'});JSON.stringify([d.first,Object.keys(d),Reflect.defineProperty(d,'other',{get:()=>1}),Reflect.preventExtensions(d)])", "[\"1\",[\"first\"],false,false]")]
        public async Task DatasetNamedPropertiesFollowTheLiveDomContract(string script, string expected)
        {
            using var context = BrowsingContext.New(Configuration.Default.WithJs());
            var document = await context.OpenAsync(response => response.Content("<body></body>")).ConfigureAwait(false);
            var engine = context.GetService<JsScriptingService>().GetOrCreateJint(document);
            engine.Execute("var d=document.body.dataset");
            var result = engine.Evaluate(script).ToString();
            Assert.AreEqual(expected, result);
        }
    }
}
