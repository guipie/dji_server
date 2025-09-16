<template>
  <div class="chatModelOptions-container">
    <el-card shadow="hover" :body-style="{ paddingBottom: '0' }">
      <el-form :model="queryParams" ref="queryForm" labelWidth="90">
        <el-row>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10">
            <el-form-item label="关键字">
              <el-input v-model="queryParams.searchKey" clearable="" placeholder="请输入模糊查询关键字"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="对应的模型">
              <el-select clearable="" filterable="" v-model="queryParams.modelId" placeholder="请选择对应的模型">
                <el-option v-for="(item,index) in  chatModelModelIdDropdownList" :key="index" :value="item.value" :label="item.label" />
                
              </el-select>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="配置名称">
              <el-input v-model="queryParams.name" clearable="" placeholder="请输入配置名称"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="配置类型">
              <el-select clearable="" v-model="queryParams.optionType" placeholder="请选择配置类型">
                <el-option v-for="(item,index) in getEnumOptionTypeData_Index" :key="index" :value="item.value" :label="`[${item.value}] ${item.describe}`" />
                
              </el-select>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="8" :xl="4" class="mb10" v-if="showAdvanceQueryUI">
            <el-form-item label="配置字段">
              <el-input v-model="queryParams.field" clearable="" placeholder="请输入配置字段"/>
              
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="12" :lg="6" :xl="6" class="mb10">
            <el-form-item>
              <el-button-group>
                <el-button type="primary"  icon="ele-Search" @click="handleQuery" v-auth="'chatModelOptions:page'"> 查询 </el-button>
                <el-button icon="ele-Refresh" @click="() => queryParams = {}"> 重置 </el-button>
                <el-button icon="ele-ZoomIn" @click="changeAdvanceQueryUI" v-if="!showAdvanceQueryUI"> 高级 </el-button>
                <el-button icon="ele-ZoomOut" @click="changeAdvanceQueryUI" v-if="showAdvanceQueryUI"> 隐藏 </el-button>
                
              </el-button-group>
              
              <el-button-group style="margin-left:20px">
                <el-button type="primary" icon="ele-Plus" @click="openAddChatModelOptions" v-auth="'chatModelOptions:add'"> 新增 </el-button>
                
              </el-button-group>
              
            </el-form-item>
            
          </el-col>
        </el-row>
      </el-form>
    </el-card>
    <el-card class="full-table" shadow="hover" style="margin-top: 8px">
      <el-table
				:data="tableData"
				style="width: 100%"
				v-loading="loading"
				tooltip-effect="light"
				row-key="id"
                @sort-change="sortChange"
				border="">
        <el-table-column type="index" label="序号" width="55" align="center"/>
        <el-table-column prop="modelId" label="对应的模型" width="120"  show-overflow-tooltip="">
          <template #default="scope">
            <span>{{scope.row.modelIdName}}</span>
            
          </template>
          
        </el-table-column>
        <el-table-column prop="name" label="配置名称" width="140"  show-overflow-tooltip="" />
          <el-table-column prop="optionType" label="配置类型" width="140"  show-overflow-tooltip="" >
            <template #default="scope">
              <el-tag>{{ getEnumDesc(scope.row.optionType, getEnumOptionTypeData_Index)}}</el-tag>
            </template>
          </el-table-column>
        <el-table-column prop="maxNum" label="最大值" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="minNum" label="最小值" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="selectedValues" label="选择的值，逗号分割" width="135"  show-overflow-tooltip="" />
        <el-table-column prop="desc" label="模型描述" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="defaultVal" label="默认值" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="extends" label="扩展" width="140"  show-overflow-tooltip="" />
        <el-table-column prop="field" label="配置字段" width="140"  show-overflow-tooltip="" />
        <el-table-column label="操作" width="140" align="center" fixed="right" show-overflow-tooltip="" v-if="auth('chatModelOptions:edit') || auth('chatModelOptions:delete')">
          <template #default="scope">
            <el-button icon="ele-Edit" size="small" text="" type="primary" @click="openEditChatModelOptions(scope.row)" v-auth="'chatModelOptions:edit'"> 编辑 </el-button>
            <el-button icon="ele-Delete" size="small" text="" type="primary" @click="delChatModelOptions(scope.row)" v-auth="'chatModelOptions:delete'"> 删除 </el-button>
          </template>
        </el-table-column>
      </el-table>
      <el-pagination
				v-model:currentPage="tableParams.page"
				v-model:page-size="tableParams.pageSize"
				:total="tableParams.total"
				:page-sizes="[10, 20, 50, 100, 200, 500]"
				small=""
				background=""
				@size-change="handleSizeChange"
				@current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper"
	/>
      <printDialog
        ref="printDialogRef"
        :title="printChatModelOptionsTitle"
        @reloadTable="handleQuery" />
      <editDialog
        ref="editDialogRef"
        :title="editChatModelOptionsTitle"
        @reloadTable="handleQuery"
      />
    </el-card>
  </div>
</template>

<script lang="ts" setup="" name="chatModelOptions">
  import { ref } from "vue";
  import { ElMessageBox, ElMessage } from "element-plus";
  import { auth } from '/@/utils/authFunction';
  import { getDictDataItem as di, getDictDataList as dl } from '/@/utils/dict-utils';
  import { formatDate } from '/@/utils/formatTime';


  import printDialog from '/@/views/system/print/component/hiprint/preview.vue'
  import editDialog from '/@/views/main/chatModelOptions/component/editDialog.vue'
  import { pageChatModelOptions, deleteChatModelOptions } from '/@/api/main/chatModelOptions';
  import { getChatModelModelIdDropdown } from '/@/api/main/chatModelOptions';
	import { getAPI } from '/@/utils/axios-utils';
	import { SysEnumApi } from '/@/api-services/api';
  import commonFunction from '/@/utils/commonFunction';

  const getEnumOptionTypeData_Index = ref<any>([]);

	const { getEnumDesc } = commonFunction();
  const showAdvanceQueryUI = ref(false);
  const printDialogRef = ref();
  const editDialogRef = ref();
  const loading = ref(false);
  const tableData = ref<any>([]);
  const queryParams = ref<any>({});
  const tableParams = ref({
    page: 1,
    pageSize: 10,
    total: 0,
  });

  const printChatModelOptionsTitle = ref("");
  const editChatModelOptionsTitle = ref("");

  // 改变高级查询的控件显示状态
  const changeAdvanceQueryUI = () => {
    showAdvanceQueryUI.value = !showAdvanceQueryUI.value;
  }
  

  // 查询操作
  const handleQuery = async () => {
    loading.value = true;
    var res = await pageChatModelOptions(Object.assign(queryParams.value, tableParams.value));
    tableData.value = res.data.result?.items ?? [];
    tableParams.value.total = res.data.result?.total;
    loading.value = false;
    getEnumOptionTypeData_Index.value = (await getAPI(SysEnumApi).apiSysEnumEnumDataListGet('AIModelOptionEnum')).data.result ?? [];
  };

  // 列排序
  const sortChange = async (column: any) => {
	queryParams.value.field = column.prop;
	queryParams.value.order = column.order;
	await handleQuery();
  };

  // 打开新增页面
  const openAddChatModelOptions = () => {
    editChatModelOptionsTitle.value = '添加模型配置';
    editDialogRef.value.openDialog({});
  };

  // 打开打印页面
  const openPrintChatModelOptions = async (row: any) => {
    printChatModelOptionsTitle.value = '打印模型配置';
  }
  
  // 打开编辑页面
  const openEditChatModelOptions = (row: any) => {
    editChatModelOptionsTitle.value = '编辑模型配置';
    editDialogRef.value.openDialog(row);
  };

  // 删除
  const delChatModelOptions = (row: any) => {
    ElMessageBox.confirm(`确定要删除吗?`, "提示", {
    confirmButtonText: "确定",
    cancelButtonText: "取消",
    type: "warning",
  })
  .then(async () => {
    await deleteChatModelOptions(row);
    handleQuery();
    ElMessage.success("删除成功");
  })
  .catch(() => {});
  };

  // 改变页面容量
  const handleSizeChange = (val: number) => {
    tableParams.value.pageSize = val;
    handleQuery();
  };

  // 改变页码序号
  const handleCurrentChange = (val: number) => {
    tableParams.value.page = val;
    handleQuery();
  };

  const chatModelModelIdDropdownList = ref<any>([]); 
  const getChatModelModelIdDropdownList = async () => {
    let list = await getChatModelModelIdDropdown();
    chatModelModelIdDropdownList.value = list.data.result ?? [];
  };
  getChatModelModelIdDropdownList();
  
  handleQuery();
</script>
<style scoped>
:deep(.el-ipnut),
:deep(.el-select),
:deep(.el-input-number) {
	width: 100%;
}
</style>

